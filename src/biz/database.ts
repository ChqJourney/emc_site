import Database from '@tauri-apps/plugin-sql';
import { writable } from 'svelte/store';
import type { Reservation, ReservationDTO, Sevent, Station, StationDTO, Test, TestDTO, Visting, VistingDTO } from '../biz/types';
import { generateLargeAmountReservationData, generateLargeAmountStationData, generateLargeAmountVistingData } from './seedData';
import { load, Store } from '@tauri-apps/plugin-store';
import { AppError, ErrorCode } from '../biz/errors';

// 创建一个可写的 store 来存储数据库连接
const db = writable<Database | null>(null);

// 初始化数据库连接
async function initDatabase() {
    const store = await load("settings.json");
    const remote_source = await store.get<string>("remote_source");
    console.log(remote_source);
    if (!remote_source || remote_source.trim() === "") {
        throw new Error("未设置远程数据源");
    }
    const dbString = `sqlite:${remote_source}\\data\\data.db?mode=rwc&cache=private`;
    // let connection_string=await invoke('get_db_connection_string');
    try {
        const database = await Database.load(dbString);
        db.set(database);

        // 检查是否需要填充数据
        await seedDataIfNeeded(database);

        console.log('Database initialized successfully');
    } catch (error) {
        console.error('Failed to initialize database:', error);
    }
}

// 添加数据填充函数
async function seedDataIfNeeded(database: Database) {
    // const isDev = import.meta.env.DEV;
    // if (!isDev) return;
    // 检查是否已经有数据
    const stations = await repository.getAllStations();
    if (stations.length === 0) {
        // 填充工位数据
        const defaultStations: StationDTO[] = await generateLargeAmountStationData();

        for (const station of defaultStations) {
            await repository.createStation(station as Station);
        }

        // 填充一些示例预约数据
        const today = new Date().toISOString().split('T')[0];
        const tomorrow = new Date(new Date().getTime() + 24 * 60 * 60 * 1000).toISOString().split('T')[0];
        const sampleReservations: ReservationDTO[] = await generateLargeAmountReservationData();

        for (const reservation of sampleReservations) {
            await repository.createReservation(reservation as Reservation);
        }
        // 填充一些示例访问记录数据
        const sampleVistings: VistingDTO[] = await generateLargeAmountVistingData();
        for (const visting of sampleVistings) {
            await repository.createVisting(visting as Visting);
        }
    }
}

// 创建 Repository 类
class Repository {
    private static instance: Repository;
    private database: Database | null = null;
    private lastAccessTime: number = 0;
    private readonly IDLE_TIMEOUT: number =30000;
    private cleanupTimer:NodeJS.Timeout | null = null;
    private constructor() {
        this.setupCleanupTimer();
        // initDatabase();
        // 添加窗口关闭事件监听器
        window.addEventListener('beforeunload', async () => {
            const database = await this.getDb();
            await database.close();
        });
    }
    private setupCleanupTimer() {
        // 定期检查空闲连接
        this.cleanupTimer = setInterval(async () => {
            if (this.database && Date.now() - this.lastAccessTime > this.IDLE_TIMEOUT) {
                console.log('已清理空闲连接');
                await this.cleanup();
                console.log(this.database)
            }
        }, 10000); // 每10秒检查一次
    }
    private async cleanup() {
        if (this.database) {
            try {
                await this.database.close();
            } catch (error) {
                console.error('Error closing database:', error);
            } finally {
                this.database = null;
            }
        }
    }
    public static getInstance(): Repository {
        if (!Repository.instance) {
            Repository.instance = new Repository();
        }
        return Repository.instance;
    }

    // 获取数据库实例
    async getDb(): Promise<Database> {
        try {
        if (!this.database) {
            const store = await Store.load("settings.json");
            const remote_source = await store.get<string>("remote_source");
            if (!remote_source?.trim()) {
                throw new Error("未设置远程数据源");
            }
            
            const dbString = `sqlite:${remote_source}\\data\\data.db?mode=ro&immutable=1&cache=private`;
            this.database = await Database.load(dbString);
            
            // 设置WAL模式和其他优化
            // await this.database.execute('PRAGMA journal_mode=WAL');
            // await this.database.execute('PRAGMA busy_timeout=5000');
            // await this.database.execute('PRAGMA synchronous=NORMAL');
        }
        
        this.lastAccessTime = Date.now();
        return this.database;
        } catch (error) {
            console.error(error)
            throw new AppError(
                ErrorCode.DB_CONNECTION_ERROR,
                '数据库连接失败',
                String(error)
            );
        }
    }

    // 示例：获取预约列表
    async getReservationsByDate(date: string): Promise<Reservation[]> {
        const database = await this.getDb();
        return await database.select(
            'SELECT * FROM reservations WHERE reservation_date = $1',
            [date]
        );
    }
    async getReservationsByMonth(month: string): Promise<Reservation[]> {
        const database = await this.getDb();
        console.log(database)
        const startDate = `${month}-01`;
        const [year, monthStr] = month.split('-');
        const lastDay = new Date(Number(year), Number(monthStr), 0).getDate();
        const endDate = `${year}-${month}-${lastDay}`;
        console.log(`SELECT * FROM reservations WHERE reservation_date>='${startDate}' AND reservation_date<='${endDate}'`);
        return await database.select(`SELECT * FROM reservations WHERE reservation_date>='${startDate}' AND reservation_date<='${endDate}' order by reservation_date desc`);
    }
    async getReservationsByYear(year: string): Promise<Reservation[]> {
        const database = await this.getDb();
        return await database.select(`SELECT * FROM reservations WHERE reservation_date>='${year}-01-01' AND reservation_date<='${year}-12-31' order by reservation_date desc`);
    }
    async getAllReservations(timeRange: string): Promise<Reservation[]> {
        const database = await this.getDb();
        switch (timeRange) {
            case 'month':
                return await this.getReservationsByMonth(`${new Date().getFullYear()}-${(new Date().getMonth() + 1).toString().padStart(2, '0')}`);
            case 'year':
                return await this.getReservationsByYear(new Date().getFullYear().toString());
            case 'all':
                return await database.select('SELECT * FROM reservations order by reservation_date desc');
            default:
                return [];
        }
    }

   
    //查询某日期某工位某时间段的预约,去除status!=normal
    async getReservationsByStationAndTime(date: string, station_id: number, time_slot: string): Promise<Reservation[]> {
        const database = await this.getDb();
        return await database.select('SELECT * FROM reservations WHERE reservation_date=$1 AND station_id=$2 AND time_slot=$3 And RESERVATION_STATUS="normal"', [date, station_id, time_slot]);
    }
    //查询某月某工位的所有预约,去除status!=normal
    async getReservationsByStationAndMonth(month: string, station_id: number): Promise<Reservation[]> {
        const database = await this.getDb();
        const startDate = `${month}-01`;
        const [year, monthStr] = month.split('-');
        const lastDay = new Date(Number(year), Number(monthStr), 0).getDate();
        const endDate = `${year}-${month}-${lastDay}`;
        return await database.select(`SELECT * FROM reservations WHERE station_id=$1 AND reservation_date>='${startDate}' AND reservation_date<='${endDate}' And RESERVATION_STATUS="normal"`, [station_id]);
    }
    //查询工位列表
    async getAllStations(): Promise<Station[]> {
        const database = await this.getDb();
        try {
            const sts= await database.select<Station[]>('SELECT * FROM stations order by created_on desc');
            console.log(sts);
            return Promise.resolve(sts); // 返回 ststs;
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询工位列表失败", String(error));
        }
    }
    
    async getStationShortNameById(id: number): Promise<string> {
        const database = await this.getDb();
        try {
            const result: { short_name: string }[] = await database.select('SELECT short_name FROM stations WHERE id=$1', [id]);
            return result[0]?.short_name || '';
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询工位名称失败", String(error));
        }
    }
    async getStationById(id: number): Promise<Station[]> {
        const database = await this.getDb();
        try {
            return await database.select('SELECT * FROM stations WHERE id=$1', [id]);
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询工位失败", String(error));
        }
    }
    async getAllSevents(): Promise<Sevent[]> {
        const database = await this.getDb();
        try {
            return await database.select('SELECT * FROM s_events order by created_on desc');
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询工位事件列表失败", String(error));
        }
    }
    async getSeventsByStationId(id: number): Promise<Sevent[]> {
        const database = await this.getDb();
        try {
            return await database.select('SELECT * FROM s_events WHERE station_id=$1 order by created_on desc', [id]);
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询工位事件列表失败", String(error));
        }
    }

    // sqls for visitings
    async getAllVistings(timeRange: string): Promise<Visting[]> {
        const database = await this.getDb();
        try {

            switch (timeRange) {
                case 'month':
                    return await this.getVistingsByMonth(`${new Date().getFullYear()}-${(new Date().getMonth() + 1).toString().padStart(2, '0')}`);
                case 'year':
                    return await this.getVistingsByYear(new Date().getFullYear().toString());
                case 'all':
                    return await database.select('SELECT * FROM visitings order by last_visit_time desc');
                default:
                    return [];
            }
        } catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询访客列表失败", String(error));
        }
    }
    async getVistingsByMonth(month: string): Promise<Visting[]> {
        const database = await this.getDb();
        try{
            return await database.select(`SELECT * FROM visitings WHERE last_visit_time>='${month}-01' AND last_visit_time<='${month}-31'`);
        }catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询访客列表失败", String(error));
        }
    }
    async getVistingsByYear(year: string): Promise<Visting[]> {
        const database = await this.getDb();
        try{
            return await database.select(`SELECT * FROM visitings WHERE last_visit_time>='${year}-01-01' AND last_visit_time<='${year}-12-31'`);
        }catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询访客列表失败", String(error));
        }
    }
    async createVisting(visting: VistingDTO) {
        const database = await this.getDb();
        try{

            return await database.execute(`INSERT INTO visitings(visit_user,visit_machine,visit_count) VALUES($1,$2,$3)`, [visting.visit_user, visting.visit_machine, visting.visit_count]);
        }catch (error) {
            throw new AppError(ErrorCode.DB_TRANSACTION_ERROR, "创建访客记录失败", String(error));
        }
    }
    async getVistingByUserAndMachine(user: string, machine: string): Promise<Visting[]> {
        const database = await this.getDb();
        try{
            return await database.select('SELECT * FROM visitings WHERE visit_user=$1 AND visit_machine=$2', [user, machine]);
        }catch (error) {
            throw new AppError(ErrorCode.DB_QUERY_ERROR, "查询访客记录失败", String(error));
        }
    }
    async updateVisting(visting: Visting) {
        const database = await this.getDb();
        try{
            return await database.execute(`UPDATE visitings SET visit_count=$1 WHERE id=$2`, [visting.visit_count, visting.id]);
        }catch (error) {
            throw new AppError(ErrorCode.DB_TRANSACTION_ERROR, "更新访客记录失败", String(error));
        }
    }
    async deleteVisiting(id: number) {
        const database = await this.getDb();
        try{
            return await database.execute(`DELETE FROM visitings WHERE id=$1`, [id]);
        }catch (error) {
            throw new AppError(ErrorCode.DB_TRANSACTION_ERROR, "删除访客记录失败", String(error));
        }
    }
    async checkDb(){
        const database = await this.getDb();
        try {
          await database.select(`SELECT 1`);
          return true;
        } catch (error) {
            console.log(error);
          return false;
        }
      }

    // 添加关闭数据库的方法
    async closeDatabase() {
        const database = await this.getDb();
        await database.close();
        // db.set(null);
    }
}

export const repository = Repository.getInstance();