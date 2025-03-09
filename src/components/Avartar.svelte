<script>
    let {username}= $props();
    

    let initials = username
        .toUpperCase()
        .slice(0, 2);
</script>

<div class="avatar">
    <span class="initials">{initials}</span>
</div>

<style>
    .avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        background-color: var(--primary-color, #4a90e2);
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: background-color 0.2s ease;
    }

    .avatar:hover {
        background-color: var(--primary-color-dark, #357abd);
    }

    .initials {
        color: white;
        font-size: 16px;
        font-weight: 500;
        user-select: none;
    }
</style>