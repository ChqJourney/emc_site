<script>
    let {username}= $props();


    let initials = username? username?.toUpperCase()?.slice(0, 2)??'':''
</script>

<div class="avatar">
    <span class="initials">{initials}</span>
</div>

<style>
    /* 基础样式（小屏幕） */
    .avatar {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        background-color: var(--primary-color, #fbc400);
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        padding: 0.4rem;
        transition: all 0.3s ease;
    }

    .initials {
        color: white;
        font-size: 12px;
        font-weight: 500;
        user-select: none;
    }

    .avatar:hover {
        background-color: var(--primary-color-dark, #fbd800);
        transform: translateY(-2px);
        box-shadow: 0 2px 8px rgba(251, 196, 0, 0.2);
    }

    /* 大屏幕样式 */
    @media (min-width: 768px) {
        .avatar {
            width: 40px;
            height: 40px;
            padding: 0.5rem;
        }

        .initials {
            font-size: 18px;
        }
    }
</style>