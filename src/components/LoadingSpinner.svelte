<script lang="ts">
  export let size: number = 40; // 默认大小
  export let color: string = "var(--color-primary, #4f46e5)"; // 使用主题色
  export let text: string = "加载中..."; // 可选文字
</script>

<div class="spinner-container">
  <div
    class="spinner"
    style="width: {size}px; height: {size}px; border-top-color: {color}; border-right-color: {color};"
  />
  {#if text}
    <div class="loading-text" style="color: {color}">{text}</div>
  {/if}
</div>

<style>
  .spinner-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
    padding: 2rem 0;
  }

  .spinner {
    border: 3px solid rgba(0, 0, 0, 0.1);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }

  .loading-text {
    font-size: 0.875rem;
    color: var(--text-secondary, #6b7280);
    letter-spacing: 0.05em;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }
</style>
