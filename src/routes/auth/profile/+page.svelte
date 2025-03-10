<script lang="ts">
  import { onMount } from 'svelte';
  import LoadingSpinner from '../../../components/LoadingSpinner.svelte';
  import { apiService } from '../../../biz/apiService';
    import Avartar from '../../../components/Avartar.svelte';
    import { goto } from '$app/navigation';
  
  let user: {
    username: string;
    machinename: string;
    englishname: string;
    team?: string;
    role?: string;
  };
  
  let loading = true;

  onMount(async () => {
    try {
      // 这里调用API获取用户数据
      const response = await apiService.Post('/auth/me');
      user = {...response};
    } catch (error) {
      console.error('Error fetching profile:', error);
    } finally {
      loading = false;
    }
  });
</script>

<div class="profile-container">
  {#if loading}
    <LoadingSpinner />
  {:else}
  <div class="profile-card">
        <div class="back-link">
          <button class="back-button" on:click={() => goto('/')}>
            <span class="back-icon">←</span> 返回首页
          </button>
        </div>
      <div class="profile-header">
        <div class="avatar-wrapper">
         <Avartar width={120} height={120} username={user?.username} />
        </div>
        <h1 class="username">{user?.username}</h1>
       
      </div>

      <div class="profile-section">
        <h2>账户信息</h2>
        <div class="info-item">
          <span class="label">用户名</span>
          <span class="value">{user?.username}</span>
        </div>
        <div class="info-item">
            <span class="label">英文名</span>
            <span class="value">{user?.englishname}</span>
            <!-- <button class="text-button" on:click={() => console.log('修改邮箱')}>修改</button> -->
          </div>
        <div class="info-item">
          <span class="label">机器名</span>
          <span class="value">{user?.machinename}</span>
          <!-- <button class="text-button" on:click={() => console.log('修改邮箱')}>修改</button> -->
        </div>
        <div class="info-item">
          <span class="label">组别</span>
          <span class="value">{user?.team || '未绑定'}</span>
          <!-- <button class="text-button" on:click={() => console.log('绑定手机')}>绑定</button> -->
        </div>
        <div class="info-item">
            <span class="label">权限</span>
            <span class="value">{user?.role || '未绑定'}</span>
            <!-- <button class="text-button" on:click={() => console.log('绑定手机')}>绑定</button> -->
          </div>
      </div>

      <div class="profile-section">
        <h2>登录安全</h2>
        <div class="info-item">
          <span class="label">密码</span>
          <span class="value">••••••••</span>
          <button class="text-button" on:click={() => goto('/auth/change-pwd')}>修改</button>
        </div>
        
      </div>
    </div>
  {/if}
</div>

<style>
  .profile-container {
    max-width: 1000px;
    margin: 2rem auto;
    padding: 0 1rem;
    width: 100%;
    box-sizing: border-box;
  }

  .back-link {
    margin-bottom: 1rem;
  }

  .back-button {
    display: flex;
    align-items: center;
    background: none;
    border: none;
    color: var(--color-primary, #4f46e5);
    font-size: 1rem;
    cursor: pointer;
    padding: 0.5rem;
    border-radius: 6px;
    transition: all 0.2s;
  }

  .back-button:hover {
    background: rgba(79, 70, 229, 0.1);
  }

  .back-icon {
    margin-right: 0.5rem;
    font-size: 1.1rem;
  }

  .profile-card {
    background: var(--card-background, #ffffff);
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    padding: 2rem;
    transition: all 0.3s ease;
  }

  .profile-header {
    text-align: center;
    margin-bottom: 1.5rem;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .avatar-wrapper {
    position: relative;
    display: inline-block;
    margin-bottom: 1rem;
  }

  .avatar {
    width: 120px;
    height: 120px;
    border-radius: 50%;
    object-fit: cover;
    border: 3px solid var(--color-primary, #4f46e5);
    transition: all 0.3s ease;
  }

  .edit-button {
    position: absolute;
    bottom: 0;
    right: 0;
    background: var(--color-primary, #4f46e5);
    color: white;
    border: none;
    border-radius: 50%;
    width: 32px;
    height: 32px;
    cursor: pointer;
    transition: transform 0.2s;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .edit-button:hover {
    transform: scale(1.1);
  }

  .username {
    color: var(--text-color, #1f2937);
    font-size: 2rem;
    margin: 0.8rem 0;
    word-break: break-word;
  }

  .edit-profile {
    background: var(--color-primary, #4f46e5);
    color: white;
    border: none;
    border-radius: 6px;
    padding: 0.5rem 1.5rem;
    font-size: 1rem;
    cursor: pointer;
    transition: all 0.2s;
  }

  .edit-profile:hover {
    opacity: 0.9;
    transform: translateY(-2px);
  }

  .profile-section {
    margin: 1.5rem 0;
    padding: 1.5rem;
    background: var(--background, #f8fafc);
    border-radius: 8px;
    transition: all 0.3s ease;
  }

  .profile-section h2 {
    color: var(--text-color, #1f2937);
    font-size: 1.5rem;
    margin-bottom: 1rem;
    border-bottom: 2px solid var(--color-primary, #4f46e5);
    padding-bottom: 0.5rem;
  }

  .info-item {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    padding: 1rem 0;
    gap: 1rem;
    border-bottom: 1px solid var(--border-color, #e5e7eb);
  }

  .info-item:last-child {
    border-bottom: none;
  }

  .label {
    flex: 1;
    min-width: 100px;
    color: var(--text-secondary, #6b7280);
    font-size: 1rem;
    font-weight: 500;
  }

  .value {
    flex: 3;
    min-width: 150px;
    color: var(--text-color, #1f2937);
    font-size: 1rem;
    word-break: break-word;
  }

  .text-button {
    background: none;
    border: none;
    color: var(--color-primary, #4f46e5);
    cursor: pointer;
    padding: 0.4rem 0.8rem;
    border-radius: 4px;
    transition: background 0.2s;
    font-size: 0.9rem;
    white-space: nowrap;
  }

  .text-button:hover {
    background: rgba(79, 70, 229, 0.1);
  }

  /* 响应式设计 */
  @media (max-width: 768px) {
    .profile-container {
      margin: 1rem auto;
    }
    
    .profile-card {
      padding: 1.5rem;
      border-radius: 8px;
    }
    
    .avatar {
      width: 100px;
      height: 100px;
    }
    
    .username {
      font-size: 1.8rem;
    }
    
    .profile-section {
      padding: 1rem;
      margin: 1rem 0;
    }
    
    .profile-section h2 {
      font-size: 1.3rem;
    }
    
    .info-item {
      flex-direction: column;
      align-items: flex-start;
      gap: 0.5rem;
    }
    
    .label {
      flex: 0 0 100%;
    }
    
    .value {
      flex: 0 0 100%;
    }
    
    .text-button {
      align-self: flex-start;
      margin-top: 0.5rem;
    }
  }

  @media (max-width: 480px) {
    .profile-card {
      padding: 1rem;
      box-shadow: 0 2px 10px rgba(0, 0, 0, 0.06);
    }
    
    .avatar {
      width: 80px;
      height: 80px;
    }
    
    .username {
      font-size: 1.5rem;
    }
    
    .edit-button {
      width: 28px;
      height: 28px;
    }
    
    .edit-profile {
      width: 100%;
      margin-top: 0.5rem;
    }
  }
</style>
