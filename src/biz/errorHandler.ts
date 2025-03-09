import { writable } from 'svelte/store';
import { AppError, ErrorCode, ErrorMessages } from '../biz/errors';

// 创建一个存储错误信息的 store
export const errorStore = writable<{
    show: boolean;
    message: string;
    type: 'error' | 'warning' | 'info';
}>({
    show: false,
    message: '',
    type: 'error'
});

// 错误处理服务
export class ErrorHandler {
    private static instance: ErrorHandler;

    private constructor() {}

    public static getInstance(): ErrorHandler {
        if (!ErrorHandler.instance) {
            ErrorHandler.instance = new ErrorHandler();
        }
        return ErrorHandler.instance;
    }

    // 处理错误
    public handleError(error: Error | AppError) {
        console.error('Error occurred:', error);

        if (error instanceof AppError) {
            // 处理自定义错误
            this.showError(`${error.message}${error.details ? ":" : ""} ${error.details ?? ""}` || ErrorMessages[error.code]);
            
            // 根据错误代码执行特定操作
            switch (error.code) {
                case ErrorCode.DB_CONNECTION_ERROR:
                    // 可以在这里添加重连逻辑
                    break;
                case ErrorCode.UNAUTHORIZED_ACCESS:
                    // 可以在这里添加重定向到登录页面的逻辑
                    break;
                // 添加其他错误代码的处理逻辑
            }
        } else {
            // 处理普通 Error
            this.showError(error.message || ErrorMessages[ErrorCode.UNKNOWN_ERROR]);
        }
    }

    // 显示错误信息
    public showError(message: string) {
        errorStore.set({
            show: true,
            message,
            type: 'error'
        });

        // 3秒后自动关闭
        setTimeout(() => {
            errorStore.set({
                show: false,
                message: '',
                type: 'error'
            });
        }, 5000);
    }

    // 显示警告信息
    public showWarning(message: string) {
        errorStore.set({
            show: true,
            message,
            type: 'warning'
        });
        // 3秒后自动关闭
        setTimeout(() => {
            errorStore.set({
                show: false,
                message: '',
                type: 'error'
            });
        }, 3000);
    }

    // 显示提示信息
    public showInfo(message: string) {
        errorStore.set({
            show: true,
            message,
            type: 'info'
        });
        // 3秒后自动关闭
        setTimeout(() => {
            errorStore.set({
                show: false,
                message: '',
                type: 'error'
            });
        }, 3000);
    }

    // 验证工具方法
    public validatePhone(phone: string): boolean {
        const phoneRegex = /^1[3-9]\d{9}$/;  // 简单的中国手机号验证
        if (!phoneRegex.test(phone)) {
            throw new AppError(
                ErrorCode.INVALID_PHONE_FORMAT,
                '请输入有效的手机号码'
            );
        }
        return true;
    }

    public validateRequired(value: any, fieldName: string): boolean {
        if (value === undefined || value === null || value === '') {
            throw new AppError(
                ErrorCode.REQUIRED_FIELD_MISSING,
                `${fieldName}不能为空`
            );
        }
        if(new Date(value) > new Date()) {
            throw new AppError( 
                ErrorCode.DATE_ELAPSED,
                '日期已过'
            );
        }
        return true;
    }
    public validateDateElapsed(value: any, fieldName: string): boolean {
        if (value === undefined || value === null || value === '') {
            throw new AppError(
                ErrorCode.REQUIRED_FIELD_MISSING,
                `${fieldName}不能为空`
            );
        }

        return true;
    }

    public validateDate(date: string): boolean {
        const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
        if (!dateRegex.test(date)) {
            throw new AppError(
                ErrorCode.INVALID_DATE_FORMAT,
                '日期格式无效，请使用YYYY-MM-DD格式'
            );
        }
        return true;
    }
}

export const errorHandler = ErrorHandler.getInstance();
