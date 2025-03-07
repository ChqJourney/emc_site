
<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { apiService } from "../../../biz/apiService";
  
    onMount(() => {
      const user = localStorage.getItem("user");
      if (!user) {
        goto("/auth/login");
      }
    });
  
    let username = '';
    let password = '';
  
    const handleLogin = async () => {
        if (username && password) {
           await apiService.login(username, password);
        }
    };
</script>

<div class="login-container">
    <div class="login-box">
        <h1>Welcome!</h1>
        <p class="subtitle">Please login to your account</p>
        <form on:submit|preventDefault={handleLogin}>
            <div class="input-group">
                <label for="username">User Name</label>
                <input id="username" bind:value={username} placeholder="Enter your PC user name" required />
                <span class="hint">Please enter your Windows login user name</span>
            </div>
            
            <div class="input-group">
                <label for="password">Password</label>
                <input type="password" id="password" bind:value={password} placeholder="Enter your password" required />
                <span class="hint">Default password is your computer name</span>
            </div>
            <div class="forgot-password">
                <a href="#" on:click={()=>alert("请联系管理员重置密码")}>Forget Password?</a>
            </div>

            <button type="submit">Sign In</button>
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
        color: #2d3748;
        font-size: 1.875rem;
        font-weight: 600;
        margin: 0;
        margin-bottom: 0.5rem;
    }

    .subtitle {
        color: #718096;
        font-size: 0.975rem;
        margin-bottom: 2rem;
    }

    .input-group {
        margin-bottom: 1.5rem;
    }

    label {
        display: block;
        color: #4a5568;
        font-size: 0.875rem;
        font-weight: 500;
        margin-bottom: 0.5rem;
    }

    input {
        width: 100%;
        padding: 0.75rem 1rem;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        font-size: 0.975rem;
        transition: all 0.2s;
    }

    input:focus {
        outline: none;
        border-color: #4299e1;
        box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.1);
    }

    input::placeholder {
        color: #a0aec0;
    }
    .forgot-password {
        text-align: right;
        margin-bottom: 1rem;
    }

    .forgot-password a {
        color: #4299e1;
        font-size: 0.875rem;
        text-decoration: none;
    }

    .forgot-password a:hover {
        text-decoration: underline;
    }
    button {
        width: 100%;
        padding: 0.75rem;
        background-color: #4299e1;
        color: white;
        border: none;
        border-radius: 6px;
        font-size: 0.975rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.2s;
    }

    button:hover {
        background-color: #3182ce;
    }

    button:focus {
        outline: none;
        box-shadow: 0 0 0 3px rgba(66, 153, 225, 0.5);
    }
    .hint {
        display: block;
        font-size: 0.75rem;
        color: #718096;
        margin-top: 0.25rem;
        font-style: italic;
    }
</style>