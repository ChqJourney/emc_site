import { repository } from "./database";
import { errorHandler } from "./errorHandler";
import { confirm, message, open } from "@tauri-apps/plugin-dialog";
import { getGlobal, setGlobal } from "./globalStore";
import { load, Store } from "@tauri-apps/plugin-store";
import { exists, readTextFile } from "@tauri-apps/plugin-fs";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { invoke } from "@tauri-apps/api/core";
const check_database_available=async(remote_source:string)=>{
  const dbConnected = await exists(`${remote_source}\\data\\data.db`);
  if (dbConnected) {
    const dbNormal=await repository.checkDb();
    console.log(dbNormal)
    if(!dbNormal){
      errorHandler.showError("数据库格式错误，无法使用");
      return false;
    }
    
    return true;
  } else {
    errorHandler.showError("数据库文件不存在");
    return false;
  }
}
const check_settings_available=async(remote_source:string)=>{
  const settingConnected = await exists(`${remote_source}\\settings.json`);
  if (settingConnected) {
    const setting_okay=await init_settings(remote_source);
    if(!setting_okay){
      return false;
    }
    return true;
  } else {
    errorHandler.showError("远程设置文件不存在");
    return false;
  }
}
const init_settings=async(remote_source:string)=>{
  try{

    const settings = await readTextFile(`${remote_source}\\settings.json`);
    const config = JSON.parse(settings);
    console.log(config);
    if(config.tests&&config.project_engineers&&config.testing_engineers&&config.station_orders){

      setGlobal("tests", config.tests);
      setGlobal("project_engineers", config.project_engineers);
      setGlobal("testing_engineers", config.testing_engineers);
      setGlobal("loadSetting", config.loadSetting);
      setGlobal("station_orders", config.station_orders);
      return true;
    }
    errorHandler.showError("设置文件信息不完整");
    return false;
  }catch(e){
    errorHandler.showError("初始化设置文件出错：" + e);
    return false;
  }
  
}


const set_remote_source=async()=>{
  const init_result = await confirm("请检查下面内容：\n 1.远程数据源当前不可用，或未设置\n 2.你未被授权\n 请重新选择远程数据源或退出", {
    title: "错误",
    kind: "warning",
  });
  if (!init_result) {
    await message("没有选择远程数据源或者远程数据源暂时不可用,无法使用本软件，软件将退出！");
    const window = await getCurrentWindow();
    await window.close();
    return;
  }
  const result = await open({
    title: "远程数据源不可用或未设置，请选择远程数据源或退出",
    directory: true,
  });
  if (result) {
    const store = await Store.load("settings.json");
    await store.set("remote_source", result);
    await message("已设置远程数据源,请重启软件已生效！");
  } else {
    await message("没有选择远程数据源或者远程数据源暂时不可用,无法使用本软件，软件将退出！");
  }
  const window = await getCurrentWindow();
  await window.close();
  return;
}
export const init = async () => {
  // 1. load settings.json from appData folder under current windows user
  const store = await Store.load("settings.json");
  // 2. load remote_source directory from settings.json
  const remote_source: string | undefined =
    await store.get<string>("remote_source");
    if(!remote_source){
      errorHandler.showError("无设置，请设置远程数据源");
      await set_remote_source();
      return;
    }
    //3. check database
    const db_ok=await check_database_available(remote_source as string);
    // 4. check settings
    const setting_ok=await check_settings_available(remote_source as string);

    

  if(!db_ok||!setting_ok){
    await set_remote_source();
    return;
  }
  setGlobal("remote_source",remote_source);
  
};
