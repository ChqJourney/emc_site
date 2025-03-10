<script>
    let {username,width=40,height=40}= $props();


    let initials = username? username?.toUpperCase()?.slice(0, 2)??'':''
</script>

<div class="avatar" style="--width: {width}px; --height: {height}px;">
    <span class="initials">{initials}</span>
</div>

<style>
    .avatar {
        width: var(--width);
        height: var(--height);
        border-radius: 50%;
        background-color: var(--primary-color, #fbc400);
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: background-color 0.2s ease;
    }

    .avatar:hover {
        background-color: var(--primary-color-dark, #fbd800);
    }

    .initials {
        color: white;
        font-size: calc(var(--width) * 0.5);
        font-weight: 500;
        user-select: none;
    }
</style>