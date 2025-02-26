use chrono::Local;
use serde_json::json;
use tauri::{Manager,Listener};
use std::{
    env,
    fs::File,
    io::{Read, Write},
};
use tauri_plugin_sql::{Migration, MigrationKind};

// Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
#[tauri::command]
fn get_db_path() -> String {
    let config = read_config().unwrap();
    config.db_path
}
#[tauri::command]
fn set_db_path(path: String) -> Result<(), String> {
    let mut config = read_config().unwrap();
    config.db_path = path;
    set_config_file(config).expect("fail to set config");
    Ok(())
}
#[tauri::command]
fn set_config(config: AppConfig) -> Result<(), String> {
    let mut config = read_config().unwrap();
    config = config;
    set_config_file(config).expect("fail to set config");
    Ok(())
}
#[tauri::command]
fn get_config() -> Result<String, String> {
    let config = read_config().unwrap();
    Ok(json!(config).to_string())
}
#[tauri::command]
fn get_db_connection_string() -> String {
    let dir = env::current_dir().unwrap();
    let config = read_config().unwrap();
    let binding = dir.join(config.db_path);
    let db_path = binding.to_str().unwrap();
    format!("sqlite:{db_path}?mode=rwc&cache=private")
}

// remember to call `.manage(MyState::default())`
#[tauri::command]
async fn check_user() -> Result<String, String> {
    let machine: String = whoami::devicename();
    let user: String = whoami::username();
    Ok(json!({"machine":machine.clone(),"user":user.clone()}).to_string())
}
#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
      
    tauri::Builder::default()
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_store::Builder::new().build())
        
        .plugin(
            tauri_plugin_sql::Builder::default()
                .build(),
        )
        .plugin(tauri_plugin_shell::init())
       
        .invoke_handler(tauri::generate_handler![
            get_db_path,
            set_db_path,
            get_db_connection_string,
            check_user,
            set_config,
            get_config
        ])
        .setup(|app| {
            let main_window = app.get_webview_window("main").unwrap();
            let loading_window = app.get_webview_window("loading").unwrap();
            
            app.listen_any("completed", move |event| {
                println!("Webview created: {:?}", event.payload());
                // 当主窗口创建完成后，显示主窗口并关闭加载窗口
                main_window.show().unwrap();
                loading_window.close().unwrap();
            });
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
fn read_config() -> Result<AppConfig, Box<dyn std::error::Error>> {
    let mut str = String::new();
    File::open("config.json")
        .expect("cannot read config.json file")
        .read_to_string(&mut str)?;
    let config: AppConfig = serde_json::from_str(&str).expect("fail to parse config.json");
    Ok(config)
}

fn set_config_file(config: AppConfig) -> Result<(), Box<dyn std::error::Error>> {
    let mut file = File::create("config.json").expect("cannot create config.json file");
    file.write_all(json!(config).to_string().as_bytes())
        .expect("cannot write to config.json file");
    Ok(())
}
#[derive(serde::Deserialize, serde::Serialize)]
pub struct AppConfig {
    pub db_path: String,
    pub low_load: i32,
    pub medium_load: i32,
    pub high_load: i32,
}
