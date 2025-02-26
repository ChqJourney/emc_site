// 错误代码枚举
export enum ErrorCode {
    // 数据库错误 (1000-1999)
    DB_CONNECTION_ERROR = 1000,
    DB_QUERY_ERROR = 1001,
    DB_TRANSACTION_ERROR = 1002,

    // 业务逻辑错误 (2000-2999)
    RESERVATION_CONFLICT = 2000,
    STATION_NOT_AVAILABLE = 2001,
    INVALID_TIME_SLOT = 2002,
    UNAUTHORIZED_ACCESS = 2003,

    // 验证错误 (3000-3999)
    INVALID_INPUT = 3000,
    REQUIRED_FIELD_MISSING = 3001,
    INVALID_DATE_FORMAT = 3002,
    INVALID_PHONE_FORMAT = 3003,

    ExportExcelFileError = 4001,

    // 系统错误 (9000-9999)
    UNKNOWN_ERROR = 9999
}

// 自定义错误类
export class AppError extends Error {
    constructor(
        public code: ErrorCode,
        public message: string,
        public details?: any
    ) {
        super(message);
        this.name = 'AppError';
    }
}

// 错误消息映射
export const ErrorMessages = {
    [ErrorCode.DB_CONNECTION_ERROR]: '数据库连接失败',
    [ErrorCode.DB_QUERY_ERROR]: '数据库查询错误',
    [ErrorCode.DB_TRANSACTION_ERROR]: '数据库事务错误',
    [ErrorCode.RESERVATION_CONFLICT]: '该时间段已被预约',
    [ErrorCode.STATION_NOT_AVAILABLE]: '工位不可用',
    [ErrorCode.INVALID_TIME_SLOT]: '无效的时间段',
    [ErrorCode.UNAUTHORIZED_ACCESS]: '未授权的访问',
    [ErrorCode.INVALID_INPUT]: '输入数据无效',
    [ErrorCode.REQUIRED_FIELD_MISSING]: '必填字段缺失',
    [ErrorCode.INVALID_DATE_FORMAT]: '日期格式无效',
    [ErrorCode.INVALID_PHONE_FORMAT]: '电话号码格式无效',
    [ErrorCode.ExportExcelFileError]: '导出Excel文件时发生错误',
    [ErrorCode.UNKNOWN_ERROR]: '未知错误'
};
