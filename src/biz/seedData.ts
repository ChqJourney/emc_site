import type { ReservationDTO, StationDTO, VistingDTO } from "../types/reservation";

export const generateSmallStationData  = async ():Promise<StationDTO[]> => {
    return  [
        {
            name: '辐射骚扰',
            short_name: '辐射骚扰',
            description: '用于电器产品辐射骚扰测试',
            photo_path: '/station_pics/emc.png',
            status: 'in_service'
        },
        {
            name: '传导骚扰',
            short_name: '传导骚扰',
            description: '用于电器产品传导骚扰测试',
            photo_path: '/station_pics/emc.png',
            status: 'in_service'
        },
    ];
}
export const generateLargeAmountStationData=async()=>{
    const stations:StationDTO[] = [];
    for(let i=0;i<20;i++){
        if(i===3||i===8||i===14){
            stations.push({
                name: `工位${i}`,
                short_name: `工位${i}`,
                description: `用于电器产品${i}测试`,
                photo_path: '/station_pics/emc.png',
                status: 'out_of_service'
            })
        }else{

            stations.push({
                name: `工位${i}`,
                short_name: `工位${i}`,
                description: `用于电器产品${i}测试`,
                photo_path: '/station_pics/emc.png',
                status: 'in_service'
            })
        }
    }
    return stations;
}
export const generateLargeAmountReservationData=async()=>{
    const reservations:ReservationDTO[] = [];
    for(let i=0;i<20;i++){
        reservations.push({
            reservation_date: `2024-11-${i>8?(i+1):'0'+(i+1)}`,
            time_slot: `T${i%4+1}`,
            client_name: `客户${i}`,
            product_name: `产品${i}`,
            station_id: i%2+1,
           reservation_status:i%2===0?'normal':'cancel',
           reservate_by:"tester"
        } as ReservationDTO)
    }
    return reservations;
}

export const generateLargeAmountVistingData=async()=>{
    const vistings:VistingDTO[] = [];
    for(let i=0;i<20;i++){
        vistings.push({
            visit_user: `sales${i}`,
            visit_machine: `sales-machine${i}`,
            visit_count: i%2+1
        } as VistingDTO)
    }
    return vistings;
}
