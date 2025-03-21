# EMC实验室工位预约管理系统

## 项目概述

EMC实验室工位预约管理系统是一个专为电磁兼容(EMC)测试实验室设计的工位预约平台。该系统允许销售人员、工程师和客户快速查看实验室工位的可用状态，并进行在线预约，从而提高了实验室资源的使用效率和工作流程的透明度。

## 功能特点

- **日历视图**：直观显示当月各工位的预约情况，颜色编码表示不同的负载状态
- **工位管理**：支持多种测试工位的信息维护与状态更新
- **预约流程**：完整的预约创建、修改和取消功能
- **客户信息管理**：记录客户和产品信息，便于追踪和统计
- **工程师分配**：支持项目工程师和测试工程师的指定
- **负载可视化**：通过颜色梯度直观展示各日期的预约负载情况
- **响应式设计**：适配各种设备屏幕尺寸

## 技术栈

- **前端**：Svelte 5 + SvelteKit 2
- **UI框架**：原生CSS
- **包管理器**：pnpm
- **构建工具**：Vite
- **后端接口**：RESTful API

## 安装与部署

### 前置条件

- Node.js (v18+)
- pnpm

### 安装步骤

1. 克隆仓库
```bash
git clone <仓库URL>
cd emc_for_sales
```

2. 安装依赖
```bash
pnpm install
```

3. 开发环境运行
```bash
pnpm dev
```

4. 构建生产版本
```bash
pnpm build
```

5. 预览生产构建
```bash
pnpm preview
```

### 环境配置

通过修改 `src/biz/apiService.ts` 中的配置来连接到后端服务：

```typescript
apiService.configure({
    baseUrl: "<后端API地址>",
    username: "<用户名>",
    machineName: "<机器名>"
});
```

## 系统使用指南

### 主页面

主页面展示日历视图，用户可以：
- 浏览不同月份的预约情况
- 通过颜色直观了解各日期的负载情况
- 点击具体日期查看或创建预约

### 日期详情页

通过点击日历上的日期进入详情页，用户可以：
- 查看该日所有预约详情
- 创建新的预约
- 修改或取消现有预约

### 工位管理页面

系统提供工位管理功能：
- 查看所有可用工位
- 工位状态设置（可用、维护中、停用等）
- 工位信息修改

## 数据模型

系统主要包含以下数据实体：

- **预约(Reservation)**：记录预约日期、时段、工位、客户等信息
- **工位(Station)**：记录工位名称、描述、状态等信息
- **测试项目(Test)**：记录可进行的测试类型
- **特殊事件(Sevent)**：记录工位的特殊安排（如停用维护等）

## 联系与支持

如有问题或建议，请联系系统管理员或发送邮件至：<管理员邮箱>

## 版权信息

© 2025 EMC实验室团队，保留所有权利
