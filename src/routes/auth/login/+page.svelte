<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { apiService } from "../../../biz/apiService";
    import { errorHandler } from "../../../biz/errorHandler";
    import type { AppError } from "../../../biz/errors";
    import axios from "axios";
  
    onMount(() => {
      // 如果已经登录并且令牌有效，直接跳转到主页
      const token = localStorage.getItem("accessToken");
      const expiresAt = localStorage.getItem("tokenExpiresAt");
      
      if (token && expiresAt && parseInt(expiresAt) > Date.now()) {
        goto("/");
      }
    });
  
    let username = '';
    let password = '';
    let isLoading = false;
    let errorMessage = '';

    const handleLogin = async () => {
        if (!username || !password) {
            errorHandler.handleError({ message: "请输入用户名和密码" } as AppError);
            return;
        }
        errorMessage = ''; // 清空旧错误
        isLoading = true;
        try {
            await apiService.login({username, password});
            goto("/");
        } catch (error) {
            console.error('登录失败:', error);
            errorMessage = '登录失败：用户名或密码错误'; // 设置错误提示
            // 可根据不同错误类型显示不同提示
            if (axios.isAxiosError(error)) {
                errorMessage = error.response?.data?.message || '服务器连接异常';
            }
        } finally {
            isLoading = false;
        }
    };
</script>

<div class="login-container">
    <div class="login-box">
        <h1>EMC实验室工位预约系统</h1>
        <p class="subtitle">请登录以访问系统</p>
        
        <form on:submit|preventDefault={handleLogin}>
            {#if errorMessage}
        <div class="error-message">
            {errorMessage}
        </div>
    {/if}
            <div class="input-group">
                <label for="username">用户名</label>
                <input 
                    type="text" 
                    id="username" 
                    bind:value={username} 
                    placeholder="输入您的用户名"
                    required
                    disabled={isLoading}
                />
            </div>
            
            <div class="input-group">
                <label for="password">密码</label>
                <input 
                    type="password" 
                    id="password" 
                    bind:value={password} 
                    placeholder="输入您的密码"
                    required
                    disabled={isLoading}
                />
            </div>
            
            <button type="submit" disabled={isLoading}>
                {#if isLoading}
                    登录中...
                {:else}
                    登录
                {/if}
            </button>
            
            <div class="links">
                <a href="/auth/change-pwd">修改密码</a>
            </div>
        </form>
    </div>
</div>

<style>
    .login-container {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        background-color: #f5f7fa;
    }

    .login-box {
        background: white;
        padding: 2.5rem;
        border-radius: 12px;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        width: 100%;
        max-width: 400px;
    }

    h1 {
        color: #333;
        margin-bottom: 0.5rem;
        text-align: center;
    }
    .error-message {
        color: #dc3545;
        background: #f8d7da;
        padding: 12px;
        border-radius: 4px;
        margin-bottom: 20px;
        border: 1px solid #f5c6cb;
        font-size: 0.9em;
    }
    .subtitle {
        color: #666;
        margin-bottom: 2rem;
        text-align: center;
    }

    .input-group {
        margin-bottom: 1.5rem;
    }

    label {
        display: block;
        margin-bottom: 0.5rem;
        color: #333;
        font-weight: 500;
    }

    input {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 1rem;
    }

    input:focus {
        outline: none;
        border-color: #4f46e5;
        box-shadow: 0 0 0 2px rgba(79, 70, 229, 0.1);
    }

    button {
        width: 100%;
        padding: 0.75rem;
        background-color: #4f46e5;
        color: white;
        border: none;
        border-radius: 4px;
        font-size: 1rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    button:hover {
        background-color: #4338ca;
    }

    button:disabled {
        background-color: #a5a5a5;
        cursor: not-allowed;
    }

    .links {
        margin-top: 1.5rem;
        text-align: center;
    }

    .links a {
        color: #4f46e5;
        text-decoration: none;
    }

    .links a:hover {
        text-decoration: underline;
    }
</style>