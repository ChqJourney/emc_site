<script lang="ts">
    import { goto } from "$app/navigation";
    import { apiService } from "../../../biz/apiService";
    import { errorHandler } from "../../../biz/errorHandler";

    let username = '';
    let currentPassword = '';
    let newPassword = '';
    let confirmPassword = '';
    let errorMessage = '';

    const handleChangePassword = async () => {
        if (newPassword !== confirmPassword) {
            errorMessage = 'New passwords do not match';
            return;
        }

        if (newPassword.length < 6) {
            errorMessage = 'New password must be at least 6 characters long';
            return;
        }

        try {
            await apiService.changePassword(username, currentPassword, newPassword);
            errorHandler.showInfo(
                'Password changed successfully. Please login with your new password.'
            )
            goto('/auth/login');
        } catch (error) {
            errorMessage = 'Failed to change password. Please try again.';
        }
    };
</script>

<div class="login-container">
    <div class="login-box">
        <h1>Change Password</h1>
        <p class="subtitle">Please enter your current and new password</p>

        {#if errorMessage}
            <div class="error-message">{errorMessage}</div>
        {/if}

        <form on:submit|preventDefault={handleChangePassword}>
            <div class="input-group">
                <label for="username">User Name</label>
                <input 
                    type="text" 
                    id="username" 
                    bind:value={username} 
                    placeholder="Enter your PC user name" 
                    required 
                />
                <span class="hint">Please enter your Windows login user name</span>
            </div>

            <div class="input-group">
                <label for="currentPassword">Current Password</label>
                <input 
                    type="password" 
                    id="currentPassword" 
                    bind:value={currentPassword} 
                    placeholder="Enter current password" 
                    required 
                />
            </div>
            
            <div class="input-group">
                <label for="newPassword">New Password</label>
                <input 
                    type="password" 
                    id="newPassword" 
                    bind:value={newPassword} 
                    placeholder="Enter new password" 
                    required 
                />
                <span class="hint">Password must be at least 6 characters long</span>
            </div>

            <div class="input-group">
                <label for="confirmPassword">Confirm New Password</label>
                <input 
                    type="password" 
                    id="confirmPassword" 
                    bind:value={confirmPassword} 
                    placeholder="Confirm new password" 
                    required 
                />
            </div>

            <button type="submit">Change Password</button>
            
            <div class="back-to-login">
                <a href="/auth/login">Back to Login</a>
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
        margin-bottom: 1rem;
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

    .error-message {
        background-color: #fed7d7;
        color: #c53030;
        padding: 0.75rem;
        border-radius: 6px;
        margin-bottom: 1.5rem;
        font-size: 0.875rem;
    }

    .back-to-login {
        text-align: center;
    }

    .back-to-login a {
        color: #4299e1;
        font-size: 0.875rem;
        text-decoration: none;
    }

    .back-to-login a:hover {
        text-decoration: underline;
    }
</style>