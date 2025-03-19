export interface Reservation {
    id: number;
    reservation_date: string;
    time_slot: string;
    station_id: number;
    client_name: string;
    product_name: string;
    contact_name: string;
    contact_phone: string;
    tests:string;
    job_no:string;
    purpose_description: string;
    sales: string;
    project_engineer: string;
    testing_engineer: string;
    updated_on:Date;
    created_on:Date;
    reservate_by: string;
    reservation_status: string;
}
export interface ReservationDTO{
    reservation_date: string;
    time_slot: string;
    station_id: number;
    client_name: string;
    product_name: string;
    contact_name: string;
    contact_phone: string;
    tests:string;
    job_no:string;
    purpose_description: string;
    sales: string;
    project_engineer: string;
    testing_engineer: string;
    reservate_by: string;
    reservation_status: string;//in_queue,cancelled,past_in_lock

}

export interface Station {
    id: number;
    name: string;
    short_name: string;
    description: string;
    photo_path: string;
    status: string;//in_service,out_of_service,maintenance
    sequence_no:number;
    updated_On:Date;
    created_On:Date;
}
export interface StationDTO{
    name: string;
    short_name: string;
    description: string;
    photo_path: string;
    status: string;
    sequence_no:number;
}
export interface Visiting {
    id: number;
    visit_user: string;
    visit_machine: string;
    visit_count: number;
    last_visit_time: string;
}
export interface VisitingDTO{
    visit_user: string;
    visit_machine: string;
    visit_count: number;
}
export interface Test{
    id:number;
    name:string;
    short_name:string;
    description:string;
    sequence_no:number;
    updated_On:Date;
    created_On:Date;
}
export interface TestDTO{
    name:string;
    short_name:string;
    description:string;
    sequence_no:number;
}
export interface User{
    username:string;
    englishname:string;
    role:string;
}
export interface Config{
    high_load:number;
    low_load:number;
    medium_load:number;
}
export interface SeventDTO{
    name:string;
    from_date:string;
    to_date:string;
    station_id:number;
    created_by:string;
    updated_by:string;
}
export interface Sevent{
    id:number;
    name:string;
    from_date:string;
    to_date:string;
    station_id:number;
    created_on:string;
    updated_on:string;
    created_by:string;
    updated_by:string;
}