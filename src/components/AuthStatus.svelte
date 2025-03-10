<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { apiService } from '../biz/apiService';
    import { goto } from '$app/navigation';
    import { errorHandler } from '../biz/errorHandler';
    import { AppError, ErrorCode } from '../biz/errors';

    // 认证状态
    let tokenExpiresAt: number | null = null;
    let timeRemaining: number = 0;
    let showWarning: boolean = false;
    let intervalId: number;
    let isRefreshing: boolean = false;

    // 初始化
    onMount(() => {
        // 获取到期时间
        updateTokenExpiresAt();
        
        // 每分钟检查一次令牌状态
        intervalId = window.setInterval(() => {
            updateTokenExpiresAt();
            checkTokenExpiration();
        }, 60000);
        
        // 初始检查
        checkTokenExpiration();
    });

    // 组件销毁时清除定时器
    onDestroy(() => {
        if (intervalId) {
            clearInterval(intervalId);
        }
    });

    // 更新令牌到期时间
    function updateTokenExpiresAt() {
        const accessToken = localStorage.getItem('accessToken');
        // 如果没有令牌，无需显示警告
        if (!accessToken) {
            showWarning = false;
            return;
        }
        
        const expiresAtStr = localStorage.getItem('tokenExpiresAt');
        tokenExpiresAt = expiresAtStr ? parseInt(expiresAtStr) : null;
        
        if (tokenExpiresAt) {
            timeRemaining = Math.max(0, Math.floor((tokenExpiresAt - Date.now()) / 60000));
        } else {
            timeRemaining = 0;
        }
    }

    // 检查令牌是否即将过期
    function checkTokenExpiration() {
        if (!tokenExpiresAt) {
            showWarning = false;
            return;
        }
        
        const timeUntilExpiration = tokenExpiresAt - Date.now();
        
        // 如果令牌已过期
        if (timeUntilExpiration <= 0) {
            // 尝试自动刷新一次，如果失败会重定向到登录页
            refreshToken();
            return;
        }
        
        // 如果令牌将在15分钟内过期，显示警告
        const fifteenMinutesInMs = 15 * 60 * 1000;
        showWarning = timeUntilExpiration > 0 && timeUntilExpiration < fifteenMinutesInMs;
    }

    // 刷新令牌
    async function refreshToken() {
        if (isRefreshing) return;
        
        try {
            isRefreshing = true;
            await apiService.refreshToken();
            updateTokenExpiresAt();
            showWarning = false;
            // 刷新成功提示
            errorHandler.showInfo('会话已成功刷新');
        } catch (error) {
            console.error('Failed to refresh token:', error);
            // 创建AppError对象
            const errorMessage = error instanceof Error ? error.message : '会话刷新失败，请重新登录';
            const appError = new AppError(
                ErrorCode.AUTH_FAILED,
                errorMessage
            );
            errorHandler.handleError(appError);
            // 等待错误提示显示后再跳转
            setTimeout(() => goto('/auth/login'), 1500);
        } finally {
            isRefreshing = false;
        }
    }

    // 登出
    async function logout() {
        try {
            // 清除认证信息
            apiService.clearAuth();
            errorHandler.showInfo('您已成功登出');
            // 等待消息显示后再跳转
            setTimeout(() => goto('/auth/login'), 1000);
        } catch (error) {
            console.error('Logout failed:', error);
            // 即使登出失败，也直接跳转到登录页
            goto('/auth/login');
        }
    }
</script>

{#if showWarning && timeRemaining > 0}
    <div class="auth-warning">
        <div class="warning-content">
            <p>您的会话将在约 {timeRemaining} 分钟后过期</p>
            <div class="warning-actions">
                <button class="refresh-button" on:click={refreshToken} disabled={isRefreshing}>
                    {#if isRefreshing}
                        刷新中...
                    {:else}
                        刷新会话
                    {/if}
                </button>
                <button class="logout-button" on:click={logout}>登出</button>
            </div>
        </div>
    </div>
{/if}

<style>
    .auth-warning {
        position: fixed;
        bottom: 20px;
        right: 20px;
        background-color: rgba(253, 224, 71, 0.95);
        border-radius: 8px;
        padding: 12px 16px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        z-index: 1000;
        max-width: 300px;
        border-left: 4px solid #f59e0b;
    }

    .warning-content p {
        margin: 0 0 10px 0;
        color: #713f12;
        font-weight: 500;
    }

    .warning-actions {
        display: flex;
        gap: 10px;
    }

    .refresh-button, .logout-button {
        padding: 6px 12px;
        border-radius: 4px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        border: none;
    }

    .refresh-button {
        background-color: #4f46e5;
        color: white;
    }

    .refresh-button:hover:not([disabled]) {
        background-color: #4338ca;
    }
    
    .refresh-button[disabled] {
        background-color: #a5a5a5;
        cursor: not-allowed;
    }

    .logout-button {
        background-color: transparent;
        color: #7c2d12;
        border: 1px solid #7c2d12;
    }

    .logout-button:hover {
        background-color: rgba(124, 45, 18, 0.1);
    }
</style> 