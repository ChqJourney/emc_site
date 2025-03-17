<script lang="ts">
    import { apiService } from "../biz/apiService";
    import { hideModal } from "./modalStore";

    interface Props {
        userInfo?: any;
        callback?: () => Promise<void>;
    }

    let { userInfo, callback } = $props();
    console.log(userInfo);
    console.log(callback);
    let isNew = !userInfo;
    let newUser = $state(
        isNew
            ? {
                  username: "",
                  machinename: "",
                  englishname: "",
                  team: "",
                  role: "User",
              }
            : {
                  username: userInfo.username,
                  machinename: userInfo.machinename,
                  englishname: userInfo.englishname,
                  team: userInfo.team,
                  role: userInfo.Role,
              },
    );
    // 创建新用户或修改
    async function createUser(e: any) {
        e.preventDefault();
        console.log(newUser);
        try {
            await apiService.Post("/auth/create", {
                username: newUser.username,
                machinename: newUser.machinename,
                englishname: newUser.englishname,
                team: newUser.team,
                role: newUser.role,
            });
            if (callback) {
                await callback();
            }
            hideModal();
        } catch (error) {
            console.error("Failed to create user:", error);
        }
    }
</script>

<div class="p-6">
    <h2 class="text-2xl font-semibold mb-4">{isNew?"创建新用户":"修改用户"}</h2>
    <form onsubmit={createUser} class="space-y-4">
        <div>
            <label for="username" class="block text-sm font-medium text-gray-700">用户名</label
            >
            <input
                id="username"
                type="text"
                bind:value={newUser.username}
                class="mt-1 block w-full rounded-md p-2 border-gray-300 border shadow-sm focus:border-blue-500 focus:ring-blue-500"
                required
            />
        </div>
        <div>
            <label for="machinename" class="block text-sm font-medium text-gray-700">机器名</label>
            <input
                id="machinename"
                type="machinename"
                bind:value={newUser.machinename}
                 class="mt-1 block w-full rounded-md p-2 border-gray-300 border shadow-sm focus:border-blue-500 focus:ring-blue-500"
                required
            />
        </div>
        <div>
            <label for="englishname" class="block text-sm font-medium text-gray-700">英文名</label
            >
            <input
                id="englishname"
                type="text"
                bind:value={newUser.englishname}
                 class="mt-1 block w-full rounded-md p-2 border-gray-300 border shadow-sm focus:border-blue-500 focus:ring-blue-500"
                required
            />
        </div>
        <div>
            <label for="team" class="block text-sm font-medium text-gray-700">组别</label
            >
            <input
                id="team"
                type="text"
                bind:value={newUser.team}
                 class="mt-1 block w-full rounded-md p-2 border-gray-300 border shadow-sm focus:border-blue-500 focus:ring-blue-500"
                required
            />
        </div>
        <div>
            <label for="role" class="block text-sm font-medium text-gray-700">角色</label>
            <select
                id="role"
                bind:value={newUser.role}
                class="mt-1 block w-full rounded-md p-2 border-gray-300 border shadow-sm focus:border-blue-500 focus:ring-blue-500"
            >
                <option value="Engineer">工程师用户</option>
                <option value="Admin">管理员</option>
                <option value="Client">只读客户</option>
            </select>
        </div>
        <div class="flex justify-end space-x-3">
            <button
                type="button"
                class="px-4 py-2 border rounded-md hover:bg-gray-50"
                onclick={() => hideModal()}
            >
                取消
            </button>
            <button
                type="submit"
                class="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
            >
                创建
            </button>
        </div>
    </form>
</div>
