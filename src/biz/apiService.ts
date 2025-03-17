// apiService.ts
import axios, { AxiosError, type AxiosInstance, type AxiosRequestConfig, type AxiosResponse } from 'axios';
import type { User } from './types';
import { setGlobal } from './globalStore';
export interface ApiServiceConfig {
    baseURL: string;
    timeout?: number;
    authEndpoints?: string[];
    storage?: {
        getItem: (key: string) => string | null;
        setItem: (key: string, value: string) => void;
        removeItem: (key: string) => void;
    };
    onAuthFail?: () => void;
}
const DEFAULT_CONFIG: Partial<ApiServiceConfig> = {
    timeout: 10000,
    authEndpoints: ['/auth/login', '/auth/refresh', '/auth/register'],
    storage: undefined
};
interface TokenData {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
}


interface RefreshTokenResponse {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
}

interface LoginPayload {
    username: string;
    password: string;
}

interface RegisterPayload {
    username: string;
    machinename: string;
    englishname: string;
    team: string;
    role: string;
}
interface RequestLog {
    timestamp: number;
    method: string;
    url: string;
    data?: any;
    response?: any;
    error?: any;
    duration: number;
}

interface ExtendedAxiosRequestConfig extends AxiosRequestConfig {
    _retry?: boolean;
    metadata?: {
        startTime: number;
    };
}

/**
 * 带超时的Promise执行
 * @param promise 要执行的Promise
 * @param timeoutMs 超时时间（毫秒）
 * @param errorMessage 超时错误消息
 */
async function withTimeout<T>(
    promise: Promise<T>,
    timeoutMs: number = 10000,
    errorMessage: string = '操作超时'
): Promise<T> {
    const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error(errorMessage)), timeoutMs);
    });

    return Promise.race([promise, timeoutPromise]);
}

class ApiService {
    private axiosInstance: AxiosInstance;
    private requestLogs: RequestLog[] = [];
    private requestThrottles = new Map<string, number>();
    private readonly THROTTLE_DELAY = 1000; // 1秒节流
    private isRefreshing = false;
    private refreshTokenPromise: Promise<RefreshTokenResponse> | null = null;
    private failedRequests: Array<() => void> = [];
    // 存储当前配置
    private currentConfig: ApiServiceConfig;

    constructor(config: ApiServiceConfig) {
        this.currentConfig = this.mergeConfig(config);
        this.axiosInstance = this.createAxiosInstance();
        this.initializeInterceptors();
    }
    public configure(newConfig: Partial<ApiServiceConfig>): void {
        // 合并新旧配置
        this.currentConfig = this.mergeConfig(newConfig);

        // 重新创建 Axios 实例
        this.axiosInstance = this.createAxiosInstance();

        // 重新绑定拦截器
        this.initializeInterceptors();
    }
    private mergeConfig(newConfig: Partial<ApiServiceConfig>): ApiServiceConfig {
        return {
            ...DEFAULT_CONFIG,
            ...this.currentConfig, // 保留现有配置
            ...newConfig           // 应用新配置
        } as ApiServiceConfig;
    }
    private createAxiosInstance(): AxiosInstance {
        return axios.create({
            baseURL: this.currentConfig.baseURL,
            timeout: this.currentConfig.timeout,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET,PUT,POST,DELETE,PATCH,OPTIONS',
                'Access-Control-Allow-Headers': 'Origin, X-Requested-With, Content-Type, Accept, Authorization'
            },
        });
    }
    private logRequest(config: ExtendedAxiosRequestConfig, response?: any, error?: any): void {
        const log: RequestLog = {
            timestamp: Date.now(),
            method: config.method?.toUpperCase() || 'UNKNOWN',
            url: config.url || 'UNKNOWN',
            data: config.data,
            response: response?.data,
            error: error?.message,
            duration: Date.now() - (config.metadata?.startTime || Date.now())
        };

        this.requestLogs.push(log);
        console.debug('[API Request]', log);
    }

    private getRequestKey(config: ExtendedAxiosRequestConfig): string {
        return `${config.method}-${config.url}-${JSON.stringify(config.data)}`;
    }

    private shouldThrottleRequest(config: ExtendedAxiosRequestConfig): boolean {
        const key = this.getRequestKey(config);
        const lastRequest = this.requestThrottles.get(key);
        const now = Date.now();

        if (lastRequest && now - lastRequest < this.THROTTLE_DELAY) {
            return true;
        }

        this.requestThrottles.set(key, now);
        return false;
    }

    public getRequestLogs(): RequestLog[] {
        return [...this.requestLogs];
    }

    public clearLogs(): void {
        this.requestLogs = [];
    }
    private initializeInterceptors() {
        // 请求拦截器
        this.axiosInstance.interceptors.request.use(
            async (config) => {
                try {
                    // 跳过刷新令牌请求的token检查，避免循环
                    const extConfig = config as ExtendedAxiosRequestConfig;
                    if (config.url?.includes('/auth/refresh-token') || extConfig._retry) {
                        return config;
                    }

                    // 检查令牌是否即将过期，如果是，则刷新
                    await this.checkTokenExpiration();

                    const token = this.currentConfig.storage?.getItem('accessToken');
                    if (token && !this.isAuthEndpoint(config.url)) {
                        config.headers.Authorization = `Bearer ${token}`;
                    }
                    return config;
                } catch (error) {
                    console.error('请求拦截器出错:', error);
                    // 确保错误被正确传递到请求链
                    return Promise.reject(error);
                }
            },
            (error) => {
                console.error('请求拦截器出错:', error);
                return Promise.reject(error);
            }
        );

        // 响应拦截器
        this.axiosInstance.interceptors.response.use(
            (response: AxiosResponse) => response,
            async (error: AxiosError) => {
                try {
                    const originalRequest = error.config as ExtendedAxiosRequestConfig;
                    if (!originalRequest) return Promise.reject(error);

                    // 处理 401 错误
                    if (error.response?.status === 401 && !originalRequest._retry) {
                        // 新增登录端点检查
                        const isLoginEndpoint = originalRequest.url?.includes('/auth/login')||originalRequest.url?.includes('/auth/refresh-token')||originalRequest.url?.includes('/auth/logout');
                        if (isLoginEndpoint) {
                            console.log('登录请求返回401，直接拒绝');
                            return Promise.reject(error);
                        }

                        originalRequest._retry = true;

                        let refreshPromise: Promise<RefreshTokenResponse>;

                        // 使用单例模式进行令牌刷新，避免多个请求同时刷新令牌
                        if (!this.isRefreshing) {
                            console.log('dd3')
                            this.isRefreshing = true;

                            // 创建一个新的刷新Promise
                            this.refreshTokenPromise = (async () => {
                                try {
                                    console.log('开始刷新令牌...');
                                    const newToken = await this.refreshToken();
                                    console.log('刷新令牌成功')
                                    // 执行所有等待的请求重试
                                    this.retryFailedRequests();

                                    return newToken;
                                } catch (error) {
                                    console.error('令牌刷新失败，清除认证状态:', error);
                                    this.clearAuth();

                                    // 通知所有等待的请求，令牌刷新失败
                                    this.failedRequests = [];

                                    if (this.currentConfig.onAuthFail) {
                                        setTimeout(() => {
                                            this.currentConfig.onAuthFail?.();
                                        }, 0);
                                    }
                                    throw error;
                                }
                            })();
                        }

                        try {
                            // 复用当前的刷新Promise
                            const newToken = await this.refreshTokenPromise;

                            // 刷新成功，更新请求头并重试
                            if (originalRequest && originalRequest.headers) {
                                originalRequest.headers.Authorization = `Bearer ${newToken?.accessToken}`;
                            }

                            // 重试原始请求
                            return await this.axiosInstance(originalRequest);
                        } catch (refreshError) {
                            // 令牌刷新失败，放弃重试，直接抛出错误
                            console.error('令牌刷新失败，无法重试原始请求:', refreshError);
                            return Promise.reject(refreshError);
                        }
                    }

                    // 对于非401错误或已经重试过的请求，直接拒绝
                    return Promise.reject(error);
                } catch (handlerError) {
                    console.error('响应拦截器错误处理异常:', handlerError);
                    return Promise.reject(error); // 返回原始错误
                }
            }
        );
    }

    // 重试所有失败的请求
    private retryFailedRequests(): void {
        // 复制一份失败请求队列，然后清空原队列
        const requests = [...this.failedRequests];
        this.failedRequests = [];

        // 逐个执行重试
        requests.forEach(retry => {
            try {
                retry();
            } catch (error) {
                console.error('重试请求失败:', error);
            }
        });
    }

    // 检查令牌是否即将过期
    private async checkTokenExpiration(): Promise<void> {
        try {
            const expiresAt = this.currentConfig.storage?.getItem('tokenExpiresAt');
            if (!expiresAt) return;

            const expirationTime = parseInt(expiresAt);
            const currentTime = Date.now();

            // 检查是否已经有刷新操作在进行
            if (this.isRefreshing) {
                console.debug('跳过令牌过期检查，刷新操作已在进行中');
                return;
            }

            // 如果令牌已过期，尝试刷新而不是直接清除
            if (currentTime > expirationTime) {
                console.warn('令牌已过期，尝试刷新');
                try {
                    // 注意：不需要再检查isRefreshing，refreshToken方法内部会处理
                    const newToken = await this.refreshToken();
                    this.currentConfig.storage?.setItem('lastTokenRefresh', Date.now().toString());
                    console.log('过期令牌刷新成功');
                } catch (error) {
                    console.error('令牌刷新失败，清除认证:', error);
                    this.clearAuth();
                    if (this.currentConfig.onAuthFail) {
                        this.currentConfig.onAuthFail();
                    }
                }
                return;
            }

            // 检查是否是刚刚获取的令牌（最近60秒内）
            const tokenAge = currentTime - (expirationTime - getTokenExpireInMs());
            if (tokenAge < 60000) { // 令牌年龄小于60秒
                console.debug('刚刚获取的令牌，跳过刷新检查');
                return;
            }

            // 令牌未过期但将在5分钟内过期，提前刷新
            const fiveMinutesInMs = 5 * 60 * 1000;
            if (expirationTime - currentTime < fiveMinutesInMs) {
                // 检查上次刷新时间，避免频繁刷新
                const lastRefreshStr = this.currentConfig.storage?.getItem('lastTokenRefresh');
                const lastRefresh = lastRefreshStr ? parseInt(lastRefreshStr) : 0;
                const refreshInterval = 60 * 1000; // 至少间隔1分钟再刷新

                if (currentTime - lastRefresh > refreshInterval) {
                    // 注意：不需要再检查isRefreshing，refreshToken方法内部会处理
                    console.log('令牌即将过期，尝试刷新');
                    try {
                        const newToken = await this.refreshToken();
                        this.currentConfig.storage?.setItem('lastTokenRefresh', Date.now().toString());
                        console.log('主动令牌刷新成功');
                    } catch (error) {
                        console.error('主动令牌刷新失败:', error);

                        // 区分不同类型的错误
                        if (error instanceof Error) {
                            if (error.message.includes('超时')) {
                                console.warn('令牌刷新超时，将在下次请求时重试');
                            } else if (error.message.includes('登录已过期')) {
                                // 令牌已失效，需要重新登录
                                this.clearAuth();
                                if (this.currentConfig.onAuthFail) {
                                    this.currentConfig.onAuthFail();
                                }
                            }
                        }
                        // 其他错误不处理，让用户继续使用当前令牌直到真正过期
                    }
                } else {
                    console.debug('跳过令牌刷新，因为最近已经刷新过');
                }
            }
        } catch (error) {
            // 捕获并记录任何意外错误，但不重抛，保持应用正常运行
            console.error('检查令牌过期时发生错误:', error);
        }
    }

    private async retryRequest(failedRequest: AxiosRequestConfig): Promise<any> {
        try {
            await this.refreshTokenPromise;
            return await this.axiosInstance(failedRequest);
        } catch (error) {
            console.log('retryRequest')
            // 明确重新抛出错误，确保调用方能捕获
            throw error;
        }
    }

    // 核心请求方法
    public async request<T = any>(config: ExtendedAxiosRequestConfig): Promise<T> {
        // 添加请求开始时间
        config.metadata = { startTime: Date.now() };

        try {
            const response = await this.axiosInstance.request<T>(config);
            this.logRequest(config, response);
            return response.data;
        } catch (error) {
            this.logRequest(config, null, error);
            
            // 由于拦截器已经处理了 401，这里不需要再处理
            if (axios.isAxiosError(error)) {
                // 记录详细错误信息以便调试
                console.error(`请求失败 [${config.method}] ${config.url}:`,
                    error.response?.status, error.response?.data);
                
                // 创建一个更有意义的错误对象
                const errorData = error.response?.data || { message: 'Network Error' };
                const status = error.response?.status || 0;
                const statusText = error.response?.statusText || 'Unknown';
                
                // 抛出一个包含更多信息的错误对象
                throw {
                    message: `请求失败: ${status} ${statusText}`,
                    status,
                    statusText,
                    data: errorData
                };
            }
            
            // 明确记录并重新抛出未知错误
            console.error('未知请求错误:', error);
            throw { message: '发生未知错误', originalError: String(error) };
        }
    }

    public async Post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
        return await this.request<T>({ method: 'post', url, data, ...config });
    }
    public async Put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
        return await this.request<T>({ method: 'put', url, data, ...config });
    }

    public async Get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return await this.request<T>({ method: 'get', url, ...config });
    }
    public async Delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return await this.request<T>({ method: 'delete', url, ...config });
    }
    // 登录方法
    public async login(payload: LoginPayload): Promise<void> {
        try {
            const response = await this.axiosInstance.post<TokenData>(
                '/auth/login',
                payload
            );
            this.storeToken(response.data);

            // 设置最后刷新时间为当前时间，防止刚登录就刷新令牌
            this.currentConfig.storage?.setItem('lastTokenRefresh', Date.now().toString());
        } catch (error) {
            console.error('登录失败:', error);
            // 确保错误传播到调用方
            throw error;
        }
    }
    public async changePassword(username: string, currentPassword: string, newPassword: string): Promise<void> {
        try {
            await this.Post('/auth/change-pwd', { userName: username, password: currentPassword, newPassword: newPassword });
        } catch (error) {
            console.error('密码修改失败:', error);
            throw error;
        }
    }

    public async logout(): Promise<void> {
        try {
            // 先发送登出请求，即使请求失败也要清除本地认证
            try {
                const tokenData = this.getToken();
                console.log("tokenData:", tokenData);
                await this.Post('/auth/logout', { accessToken: tokenData.accessToken, refreshToken: tokenData.refreshToken });
            } catch (error) {
                console.warn('服务器登出请求失败，但会继续清除本地认证:', error);
            }finally{

                
                // 无论如何都清除本地认证状态
                this.clearAuth();
            }
        } catch (error) {
            console.error('登出过程发生错误:', error);
            // 确保在错误情况下也清除本地认证
            this.clearAuth();
            throw error;
        }
    }

    public async list(): Promise<User[]> {
        try {
            const response = await this.axiosInstance.get<User[]>(
                '/auth/list'
            );
            return response.data;
        } catch (error) {
            console.error('获取用户列表失败:', error);
            throw error;
        }
    }

    // 注册方法
    public async create(payload: RegisterPayload): Promise<void> {
        try {
            await this.Post('/auth/create', payload);
        } catch (error) {
            console.error('创建用户失败:', error);
            throw error;
        }
    }

    // Token 刷新方法
    public async refreshToken(): Promise<RefreshTokenResponse> {
        // 添加锁，确保状态同步
        const lockPromise = new Promise<void>((resolve) => setTimeout(resolve, 0));
        await lockPromise;

        // 添加互斥锁，防止并发调用
        if (this.isRefreshing) {
            console.warn('刷新令牌操作正在进行中，等待完成...');

            // 等待现有刷新完成
            if (this.refreshTokenPromise) {
                console.log('dd')
                try {
                    console.log('dd2')
                    return await this.refreshTokenPromise;
                } catch (error) {
                    console.error('等待中的刷新令牌请求失败:', error);
                    throw error;
                }
            } else {
                // 设置刷新状态不一致，重置刷新状态
                console.warn('刷新状态不一致，重置刷新状态');
                this.isRefreshing = false;
            }
        }
        console.log('dd2')

        try {
            // 设置互斥锁
            this.isRefreshing = true;

            // 创建一个新的Promise用于其他调用等待
            this.refreshTokenPromise = (async () => {
                try {
                    const tokenData = this.getToken();
                    if (!tokenData.accessToken) throw new Error('No access token available');
                    if (!tokenData.refreshToken) throw new Error('No refresh token available');

                    // 详细日志记录刷新令牌请求的数据
                    console.debug('开始刷新令牌，当前时间：', new Date().toISOString());
                    console.debug('令牌过期时间：', new Date(tokenData.expiresIn).toISOString());
                    console.debug('刷新令牌请求详情', {
                        当前时间: new Date().toISOString(),
                        过期时间: new Date(tokenData.expiresIn).toISOString(),
                        accessToken截取: tokenData.accessToken.substring(0, 10) + '...',
                        refreshToken截取: tokenData.refreshToken.substring(0, 10) + '...'
                    });

                    // 检查API接口的实际需求 - 某些API可能只需要refreshToken而不需要accessToken
                    // 此处修改为仅发送refreshToken
                    const newToken = await withTimeout(
                        this.Post<RefreshTokenResponse>(
                            '/auth/refresh-token',
                            { refreshToken: tokenData.refreshToken, accessToken: tokenData.accessToken },
                            { _retry: true } as ExtendedAxiosRequestConfig
                        ),
                        15000,
                        '刷新令牌请求超时'
                    );

                    console.debug('令牌刷新成功，新过期时间：', new Date(newToken.expiresIn).toISOString());

                    // 保存新令牌前记录旧令牌和新令牌的差异
                    console.debug('令牌更新前后对比:', {
                        旧刷新令牌: tokenData.refreshToken.substring(0, 10) + '...',
                        新刷新令牌: newToken.refreshToken.substring(0, 10) + '...'
                    });

                    // 更新存储的令牌
                    this.storeToken({
                        accessToken: newToken.accessToken,
                        refreshToken: newToken.refreshToken,
                        expiresIn: newToken.expiresIn
                    });

                    return newToken;
                } catch (error) {
                    // 处理API请求错误
                    console.error('刷新令牌API请求失败：', error);

                    // 添加更详细的错误日志以帮助诊断
                    if (axios.isAxiosError(error)) {
                        console.error('刷新令牌请求详情:', {
                            状态码: error.response?.status,
                            错误信息: error.response?.data,
                            请求URL: error.config?.url,
                            请求方法: error.config?.method
                        });

                        if (error.response?.status === 400 || error.response?.status === 401) {
                            console.warn('刷新令牌无效或已过期，需要重新登录');
                            this.clearAuth();
                            throw new Error('您的登录已过期，请重新登录');
                        }
                    }

                    // 如果是超时错误
                    if (error instanceof Error && error.message.includes('超时')) {
                        console.warn('刷新令牌请求超时');
                        throw new Error('网络请求超时，请检查您的网络连接');
                    }

                    throw error;
                }
            })();

            return await this.refreshTokenPromise;
        } finally {
            // 请求完成后（无论成功还是失败）一定要重置状态
            // 使用setTimeout确保在所有await刷新令牌的地方都能得到结果后再重置状态
            setTimeout(() => {
                this.isRefreshing = false;
                this.refreshTokenPromise = null;
                console.debug('刷新令牌操作完成，重置刷新状态');
            }, 0);
        }
    }

    private storeToken(tokenData: TokenData): void {
        // 存储令牌信息
        this.currentConfig.storage?.setItem('accessToken', tokenData.accessToken);
        this.currentConfig.storage?.setItem('refreshToken', tokenData.refreshToken);

        // 修正过期时间的计算和存储
        // 如果expiresIn是时间戳，则直接使用；如果是秒数，则转换为时间戳
        const expiresAt = tokenData.expiresIn > Date.now()
            ? tokenData.expiresIn  // 已经是时间戳
            : Date.now() + (tokenData.expiresIn * 1000);  // 是秒数，转换为时间戳

        this.currentConfig.storage?.setItem('tokenExpiresAt', expiresAt.toString());

        // 记录验证信息
        console.debug('令牌已存储', {
            存储时间: new Date().toISOString(),
            过期时间: new Date(expiresAt).toISOString(),
            有效期: Math.round((expiresAt - Date.now()) / 1000 / 60) + '分钟'
        });
    }
    private getToken(): TokenData {
        const accessToken = this.currentConfig.storage?.getItem('accessToken') || '';
        const refreshToken = this.currentConfig.storage?.getItem('refreshToken') || '';
        const expiresAtStr = this.currentConfig.storage?.getItem('tokenExpiresAt') || '0';

        // 确保expiresIn是时间戳
        const expiresIn = parseInt(expiresAtStr);

        // 记录更多调试信息
        const now = Date.now();
        if (accessToken && refreshToken) {
            console.debug('获取令牌信息', {
                当前时间: new Date(now).toISOString(),
                过期时间: new Date(expiresIn).toISOString(),
                剩余有效期: Math.round((expiresIn - now) / 1000 / 60) + '分钟'
            });
        }

        return { accessToken, refreshToken, expiresIn };
    }

    // 清除认证信息
    public clearAuth(): void {
        this.currentConfig.storage?.removeItem('accessToken');
        this.currentConfig.storage?.removeItem('refreshToken');
        this.currentConfig.storage?.removeItem('tokenExpiresAt');
        setGlobal("user",null);
    }
    private isAuthEndpoint(url?: string): boolean {
        if (!url) return false;
        return this.currentConfig?.authEndpoints?.some(endpoint =>
            url.startsWith(endpoint) ||
            url.startsWith(this.currentConfig.baseURL + endpoint)
        ) ?? false;
    }

    private isTokenExpiredError(error: AxiosError): boolean {
        return error.response?.status === 401 &&
            error.config?.url !== '/auth/login' &&
            error.config?.url !== '/auth/refresh';
    }

}

// 使用示例
export const apiService = new ApiService({
    baseURL: '',  // 使用相对路径，请求会基于当前域名
    timeout: 5000,
    storage: undefined
});

export const checkAuth = async () => {
    try {
        // 检查是否有令牌
        const accessToken = localStorage.getItem('accessToken');
        const refreshToken = localStorage.getItem('refreshToken');
        const expiresAt = localStorage.getItem('tokenExpiresAt');

        // 如果缺少任何令牌信息，表示未认证
        if (!accessToken || !refreshToken || !expiresAt) {
            console.warn('缺少认证信息，需要重新登录', {
                hasAccessToken: !!accessToken,
                hasRefreshToken: !!refreshToken,
                hasExpiresAt: !!expiresAt
            });
            apiService.clearAuth();
            return false;
        }

        const expirationTime = parseInt(expiresAt);
        const currentTime = Date.now();

        // 记录当前令牌状态
        console.debug('令牌状态检查', {
            当前时间: new Date(currentTime).toISOString(),
            过期时间: new Date(expirationTime).toISOString(),
            是否过期: currentTime > expirationTime,
            剩余时间: Math.round((expirationTime - currentTime) / 1000 / 60) + '分钟'
        });

        // 检查是否已经有刷新操作在进行
        if (apiService['isRefreshing']) {
            console.log('令牌刷新已在进行中，等待结果...');
            try {
                // 等待现有刷新操作完成
                if (apiService['refreshTokenPromise']) {
                    await apiService['refreshTokenPromise'];
                    console.log('等待中的刷新令牌操作完成');
                    return true;
                } else {
                    // 如果发现状态不一致，让apiService自行处理
                    console.warn('刷新令牌状态不一致');
                    // 等待一段时间，让可能的竞争条件解决
                    await new Promise(resolve => setTimeout(resolve, 100));
                }
            } catch (error) {
                console.error('等待刷新令牌时出错:', error);
                // 继续执行，尝试自己刷新令牌
            }
        }

        // 如果令牌已过期，尝试刷新
        if (currentTime > expirationTime) {
            console.log('令牌已过期，尝试刷新');
            try {
                const result = await apiService.refreshToken();
                console.log('令牌刷新成功', {
                    新过期时间: new Date(result.expiresIn).toISOString()
                });
                return true;
            } catch (error) {
                console.error('令牌刷新失败，需要重新登录:', error);
                // 仅在这里清除认证，避免多处清除
                apiService.clearAuth();
                return false;
            }
        } else {
            // 检查是否是刚刚登录（最近60秒内获取的令牌）
            const tokenAge = currentTime - (expirationTime - getTokenExpireInMs());
            if (tokenAge < 60000) { // 令牌年龄小于60秒
                console.log('刚刚获取的令牌，跳过刷新检查');
                return true;
            }

            // 令牌未过期但将在5分钟内过期，提前刷新
            const fiveMinutesInMs = 5 * 60 * 1000;
            if (expirationTime - currentTime < fiveMinutesInMs) {
                // 检查上次刷新时间，避免频繁刷新
                const lastRefreshStr = localStorage.getItem('lastTokenRefresh');
                const lastRefresh = lastRefreshStr ? parseInt(lastRefreshStr) : 0;
                const refreshInterval = 60 * 1000; // 至少间隔1分钟再刷新

                if (currentTime - lastRefresh > refreshInterval) {
                    // 如果已经有刷新操作在进行，则不重复刷新
                    if (apiService['isRefreshing']) {
                        console.debug('跳过提前刷新，因为另一个刷新操作正在进行');
                        return true;
                    }

                    console.log('令牌即将过期，提前刷新');
                    try {
                        const result = await apiService.refreshToken();
                        localStorage.setItem('lastTokenRefresh', currentTime.toString());
                        console.log('令牌提前刷新成功', {
                            新过期时间: new Date(result.expiresIn).toISOString()
                        });
                    } catch (error) {
                        // 这里不清除认证，仍然使用当前有效的令牌
                        console.warn('提前刷新令牌失败，但当前令牌仍然有效:', error);
                    }
                } else {
                    console.debug('跳过提前刷新，因为最近已经刷新过');
                }
            }
            // 令牌有效
            return true;
        }
    } catch (error) {
        console.error('检查认证状态时发生错误:', error);
        // 出现未预期的错误，清除认证状态以确保安全
        apiService.clearAuth();
        return false;
    }
}

// 获取配置的令牌过期时间（毫秒）
function getTokenExpireInMs(): number {
    // 默认30分钟
    return 30 * 60 * 1000;
}