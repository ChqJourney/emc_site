// apiService.ts
import axios, { AxiosError, type AxiosInstance, type AxiosRequestConfig, type AxiosResponse } from 'axios';
import type { User } from './types';
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
            (config) => {
                const token = this.currentConfig.storage?.getItem('accessToken');
                if (token && !this.isAuthEndpoint(config.url)) {
                    config.headers.Authorization = `Bearer ${token}`;
                }
                return config;
            },
            (error) => Promise.reject(error)
        );

        // 响应拦截器
        this.axiosInstance.interceptors.response.use(
            (response: AxiosResponse) => response,
            async (error: AxiosError) => {
                const originalRequest = error.config as ExtendedAxiosRequestConfig;
                if (!originalRequest) return Promise.reject(error);

                // 处理 401 错误
                if (error.response?.status === 401&& !originalRequest._retry) {
                    originalRequest._retry = true;  // 标记该请求已经重试过
                    
                    if (!this.isRefreshing) {
                        this.isRefreshing = true;
                        this.refreshTokenPromise = (async () => {
                            try {
                                const newToken = await this.refreshToken();
                                const tokenData = this.getToken();
                                this.storeToken({ 
                                    ...tokenData, 
                                    accessToken: newToken.accessToken, 
                                    refreshToken: newToken.refreshToken,
                                    expiresIn: newToken.expiresIn 
                                });
                                return newToken;
                            } catch (error) {
                                this.clearAuth();
                                if (this.currentConfig.onAuthFail) {
                                    this.currentConfig.onAuthFail();
                                }
                                throw error;
                            } finally {
                                this.isRefreshing = false;
                                this.refreshTokenPromise = null;
                            }
                        })();
                    }
    
                    try {
                        const newToken = await this.refreshTokenPromise;
                        // 更新失败请求的 Authorization header
                        if (originalRequest && originalRequest.headers) {
                            originalRequest.headers.Authorization = `Bearer ${newToken?.accessToken}`;
                        }
                        return this.axiosInstance(originalRequest);
                    } catch (error) {
                        // token 刷新失败，直接抛出错误，不再重试
                    this.clearAuth();
                    if (this.currentConfig.onAuthFail) {
                        this.currentConfig.onAuthFail();
                    }
                        return Promise.reject(error);
                    }
                }
                return Promise.reject(error);
            }
        );
    }
    private async retryRequest(failedRequest: AxiosRequestConfig): Promise<any> {
        try {
            await this.refreshTokenPromise;
            return await this.axiosInstance(failedRequest);
        } catch (error) {
            throw error;
        }
    }

    // 核心请求方法
    public async request<T = any>(config: ExtendedAxiosRequestConfig): Promise<T> {
        // 添加请求开始时间
        config.metadata = { startTime: Date.now() };

        // 检查节流
        // if (this.shouldThrottleRequest(config)) {
        //     const error = new Error('Request throttled');
        //     this.logRequest(config, undefined, error);
        //     throw error;
        // }
        try {
            const response = await this.axiosInstance.request<T>(config);
            return response.data;
        } catch (error) {
             // 由于拦截器已经处理了 401，这里不需要再处理
        if (axios.isAxiosError(error)) {
            throw error.response?.data || { error: 'Network Error' };
        }
        throw { error: 'Unknown Error' };
        }
    }

    public async Post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
        return this.request<T>({ method: 'post', url, data, ...config });
    }
    public async Get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
        return this.request<T>({ method: 'get', url, ...config });
    }
    // 登录方法
    public async login(payload: LoginPayload): Promise<void> {
        const response = await this.axiosInstance.post<TokenData>(
            '/auth/login',
            payload
        );
        this.storeToken(response.data);
    }
    public async logout(username: string): Promise<void> {
        await this.Post('/auth/logout', { username });
        this.clearAuth();
    }
    public async list(): Promise<User[]> {
        const response = await this.axiosInstance.get<User[]>(
            '/auth/list'
        );
        console.log(response.data)
        return response.data;
    }
    // 注册方法
    public async create(payload: RegisterPayload): Promise<void> {
        await this.Post('/auth/create', payload);
    }

    // Token 刷新方法
    public async refreshToken(): Promise<RefreshTokenResponse> {
        const tokenData = this.getToken();
        if (!tokenData.accessToken) throw new Error('No access token available');
        if (!tokenData.refreshToken) throw new Error('No refresh token available');
        console.log('Refresh Token Request Payload:', {
            accessToken: tokenData.accessToken,
            refreshToken: tokenData.refreshToken
        });
        const newToken = await this.Post<RefreshTokenResponse>(
            '/auth/refresh-token',
            { accessToken: tokenData.accessToken, refreshToken: tokenData.refreshToken }
        );
        console.log('Refresh Token Response:', newToken);
        return newToken;
    }

    private storeToken(tokenData: TokenData): void {
        this.currentConfig.storage?.setItem('accessToken', tokenData.accessToken);
        this.currentConfig.storage?.setItem('refreshToken', tokenData.refreshToken);
        this.currentConfig.storage?.setItem('tokenExpiresAt', tokenData.expiresIn.toString());
    }
    private getToken(): TokenData {
        return {
            accessToken: this.currentConfig.storage?.getItem('accessToken') ?? '',
            refreshToken: this.currentConfig.storage?.getItem('refreshToken') ?? '',
            expiresIn: this.currentConfig.storage?.getItem('tokenExpiresAt') ? parseInt(this.currentConfig.storage?.getItem('tokenExpiresAt') ?? '0') : 0
        }
    }

    // 清除认证信息
    public clearAuth(): void {
        this.currentConfig.storage?.removeItem('accessToken');
        this.currentConfig.storage?.removeItem('refreshToken');
        this.currentConfig.storage?.removeItem('tokenExpiresAt');
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

    private retryFailedRequests(): void {
        while (this.failedRequests.length) {
            const retry = this.failedRequests.shift();
            retry?.();
        }
    }


}

// 使用示例
export const apiService = new ApiService({
    baseURL: 'https://api.example.com',
    timeout: 5000
});

export const checkAuth = async () => {
    const expiresAt = localStorage.getItem('tokenExpiresAt');
    if (!expiresAt) return false;
    const expirationTime = parseInt(expiresAt);
    console.log('Current time:', Date.now());
    console.log('Expiration time:', expirationTime);
    console.log('Time difference (seconds):', (Date.now() - expirationTime) / 1000);
    if (expiresAt && Date.now() > parseInt(expiresAt)) {
        try {
            await apiService.refreshToken();
            return true;
        } catch (error) {
            console.error('Token refresh failed:', error);
            apiService.clearAuth();
            return false;
        }
    } else {
        return true;
    }

}