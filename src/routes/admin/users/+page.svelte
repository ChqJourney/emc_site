<script lang="ts">
    import { onMount, type Component } from "svelte";
    import { apiService } from "../../../biz/apiService";
    import { showModal } from "../../../components/modalStore";
    import UserForm from "../../../components/UserForm.svelte";

    let users: any = $state([]);
    let currentPage = $state(1);
    let pageSize = $state(10);
    let totalPages = $state(0);
    let loading = $state(false);
    let isDeleting = $state(false);

    // 新用户表单数据
    let newUser = {
        username: "",
        password: "",
        role: "User",
    };

    // 加载用户列表
    async function loadUsers() {
        users = [];
        try {
            loading = true;
            const response = await apiService.list();

            console.log(response);
            if (!response) {
            } else {
                users = response;
                totalPages = Math.ceil(users.length / pageSize);
            }
        } catch (error) {
            console.error("Failed to load users:", error);
        } finally {
            loading = false;
        }
    }

    // 获取当前页的用户

    let paginatedUsers = $derived(
        users.length > 0
            ? users.slice((currentPage - 1) * pageSize, currentPage * pageSize)
            : [],
    );

    // 重置用户密码
    async function resetPassword(username: string) {
        if (confirm(`确定要重置用户 ${username} 的密码吗？`)) {
            try {
                await apiService.Post(
                    `/auth/reset-password?username=${username}`,
                );
                alert("密码重置成功");
            } catch (error) {
                console.error("Failed to reset password:", error);
            }
        }
    }

    // 删除用户
    async function deleteUser(userId: string, username: string) {
        if (confirm(`确定要删除用户 ${username} 吗？此操作不可恢复！`)) {
            try {
                isDeleting = true;
                await apiService.Delete(`/auth/remove/${userId}`);
                await loadUsers();
                alert("用户已删除");
            } catch (error) {
                console.error("Failed to delete user:", error);
            } finally {
                isDeleting = false;
            }
        }
    }

    // 锁定/解锁用户
    async function toggleUserLock(
        userId: string,
        username: string,
        isLocked: boolean,
    ) {
        const action = isLocked ? "解锁" : "锁定";
        if (confirm(`确定要${action}用户 ${username} 吗？`)) {
            try {
                await apiService.Put(`/auth/lock/${userId}`, {
                    isLocked: !isLocked,
                });
                await loadUsers();
                alert(`用户已${action}`);
            } catch (error) {
                console.error(`Failed to ${action} user:`, error);
            }
        }
    }

    onMount(loadUsers);
</script>

<div class="container mx-auto p-4">
    <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-semibold">用户管理</h1>
        <button
            class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg transition-colors"
            onclick={() =>
                showModal(UserForm as unknown as Component<{}, {}, "">, {
                    userInfo: undefined,
                    callback: async () => await loadUsers(),
                })}
        >
            添加用户
        </button>
    </div>

    {#if loading}
        <div class="flex justify-center items-center h-32">
            <div
                class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"
            ></div>
        </div>
    {:else}
        <div class="bg-white rounded-lg shadow overflow-hidden">
            <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                    <tr>
                        <th
                            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            >用户名</th
                        >
                        <th
                            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            >角色</th
                        >
                        <th
                            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            >机器名</th
                        >
                        <th
                            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            >状态</th
                        >
                        <th
                            class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                            >操作</th
                        >
                    </tr>
                </thead>
                <tbody
                    class="bg-white divide-y font-normal text-sm text-gray-500 divide-gray-200"
                >
                    {#each paginatedUsers as user}
                        <tr>
                            <td class="px-6 py-4 whitespace-nowrap"
                                >{user.username}</td
                            >
                            <td class="px-6 py-4 whitespace-nowrap"
                                >{user.role}</td
                            >
                            <td class="px-6 py-4 whitespace-nowrap"
                                >{user.machinename}</td
                            >
                            <td class="px-6 py-4 whitespace-nowrap">
                                {#if !user.isActive}
                                    <span
                                        class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800"
                                    >
                                        已锁定
                                    </span>
                                {:else}
                                    <span
                                        class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800"
                                    >
                                        活跃
                                    </span>
                                {/if}
                            </td>
                            <td
                                class="px-6 py-4 whitespace-nowrap text-sm text-gray-500"
                            >
                                <button
                                    class="text-blue-600 hover:text-blue-800 mr-3"
                                    onclick={() => resetPassword(user.username)}
                                    disabled={isDeleting}
                                >
                                    重置密码
                                </button>
                                <button
                                    class="text-yellow-600 hover:text-yellow-800 mr-3"
                                    onclick={() =>
                                        toggleUserLock(
                                            user.id,
                                            user.username,
                                            user.isActive,
                                        )}
                                    disabled={isDeleting}
                                >
                                    {user.isActive ? "锁定" : "解锁"}
                                </button>
                                <button
                                    class="text-red-600 hover:text-red-800"
                                    onclick={() =>
                                        deleteUser(user.id, user.username)}
                                    disabled={isDeleting}
                                >
                                    删除
                                </button>
                            </td>
                        </tr>
                    {/each}
                </tbody>
            </table>
        </div>

        <!-- 分页控件 -->
        <div class="flex justify-center mt-4 space-x-2">
            <button
                class="px-3 py-1 rounded-md bg-gray-100 disabled:opacity-50"
                disabled={currentPage === 1}
                onclick={() => currentPage--}
            >
                上一页
            </button>
            <span class="px-3 py-1">{currentPage} / {totalPages}</span>
            <button
                class="px-3 py-1 rounded-md bg-gray-100 disabled:opacity-50"
                disabled={currentPage === totalPages}
                onclick={() => currentPage++}
            >
                下一页
            </button>
        </div>
    {/if}
</div>
