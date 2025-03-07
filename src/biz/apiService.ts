import { errorHandler } from './errorHandler';
import { AppError, ErrorCode } from './errors';

interface ApiServiceConfig {
    baseUrl: string;
    username?: string;
    machineName?: string;
}

interface RequestOptions extends RequestInit {
    skipAuth?: boolean;
}

export class ApiService {
    private static instance: ApiService;
    private baseUrl: string = '';
    private username: string | undefined;
    private password: string | undefined;

    private constructor() {}

    public static getInstance(): ApiService {
        if (!ApiService.instance) {
            ApiService.instance = new ApiService();
        }
        return ApiService.instance;
    }

    public configure(config: ApiServiceConfig): void {
        this.baseUrl = config.baseUrl.endsWith('/') ? config.baseUrl.slice(0, -1) : config.baseUrl;
        this.username = config.username;
        this.password = config.machineName;
    }

    private getAuthHeaders(): HeadersInit {
        const headers: HeadersInit = {
            'Content-Type': 'application/json',
        };

        if (this.username) {
            headers['username'] = this.username;
        }
        if (this.password) {
            headers['password'] = this.password;
        }
        
        return headers;
    }


    private async handleResponse<T>(response: Response): Promise<T> {
        if (!response.ok) {
            let errorMessage: string;
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || response.statusText;
            } catch {
                errorMessage = response.statusText;
            }

            throw new AppError(
                ErrorCode.API_ERROR,
                `API request failed: ${errorMessage}`,
                { status: response.status }
            );
        }

        try {
            return await response.json();
        } catch (error) {
            throw new AppError(
                ErrorCode.API_PARSE_ERROR,
                'Failed to parse API response',
                { originalError: error }
            );
        }
    }

    public async fetch<T = any>(
        endpoint: string,
        options: RequestOptions = {}
    ): Promise<T> {
        try {
            const url = endpoint.startsWith('http') 
                ? endpoint 
                : `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

            const headers = !options.skipAuth
                ? { ...this.getAuthHeaders(), ...(options.headers || {}) }
                : options.headers || {};

            const response = await fetch(url, {
                ...options,
                headers
            });

            return await this.handleResponse<T>(response);
        } catch (error) {
            if (error instanceof AppError) {
                throw error;
            }
            
            throw new AppError(
                ErrorCode.API_REQUEST_ERROR,
                'API request failed',
                { originalError: error }
            );
        }
    }
    public async login(username: string, password: string): Promise<void> {
        try {
            const response = await this.post('/auth', 
                { UserName:username, Password:password }, 
                { skipAuth: true,headers:{'Content-Type': 'application/json'} });
            if (response.success) {
                this.username = username;
                this.password = password;
            } else {
                throw new AppError(ErrorCode.AUTH_FAILED, 'Login failed');
            }
        } catch (error) {
            errorHandler.handleError(error as AppError);
        }
    }
    public async changePassword(userName:string,oldPassword: string, newPassword: string): Promise<void> {
        try {
            const response = await this.put('/auth',
                {UserName:userName, Password: oldPassword, NewPassword: newPassword });
            if (!response.success) {
                throw new AppError(ErrorCode.AUTH_FAILED, 'Change password failed');
            }
        } catch (error) {
            errorHandler.handleError(error as AppError);
        }
    }
    public async logout(): Promise<void> {
        this.username = undefined;
        this.password = undefined;
    }

    // 便捷方法
    public async get<T = any>(endpoint: string, options: RequestOptions = {}): Promise<T> {
        return this.fetch<T>(endpoint, { ...options, method: 'GET' });
    }

    public async post<T = any>(endpoint: string, data?: any, options: RequestOptions = {}): Promise<T> {
        return this.fetch<T>(endpoint, {
            ...options,
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    public async put<T = any>(endpoint: string, data?: any, options: RequestOptions = {}): Promise<T> {
        return this.fetch<T>(endpoint, {
            ...options,
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    public async delete<T = any>(endpoint: string, options: RequestOptions = {}): Promise<T> {
        return this.fetch<T>(endpoint, { ...options, method: 'DELETE' });
    }
}

export const apiService = ApiService.getInstance();

// 立即配置apiService，无需等待onMount
apiService.configure({
    baseUrl: "http://localhost:5000/api",
    username: "patri",
    machineName: "pwin"
});
