#pragma once
#define NOMINMAX
#include <windows.h>
#include <filesystem>
#include <functional>
#include <atomic>
#include <string>
#include <vector>
#include "vendor/json.hpp"
namespace wr {
namespace fs=std::filesystem;
using json=nlohmann::json;
using Progress=std::function<void(const std::string&,uint64_t,uint64_t)>;
inline constexpr int LauncherSchema=1;
inline constexpr const char* CatalogURL="https://github.com/danemoll-jpg/chastain-rush-releases/releases/latest/download/update-catalog.json";
std::wstring Wide(const std::string& s);
std::string Utf8(const std::wstring& s);
std::string Read(const fs::path& path,size_t maxBytes=8*1024*1024);
void AtomicWrite(const fs::path& path,const std::string& data);
std::string Hash(const fs::path& path);
std::string HashBytes(const std::string& data);
json Verify(const std::string& envelope);
fs::path SafePath(const fs::path& root,const std::string& relative);
void CheckNoLinks(const fs::path& path);
void ProtectRoot(const fs::path& root);
std::vector<BYTE> Base64Decode(const std::string& input);
void CheckInventory(const fs::path& root,const json& files,bool exact=true);
void Extract(const fs::path& archive,const fs::path& destination,const json& files);
void Download(const std::string& url,const fs::path& output,uint64_t expectedBytes,const std::string& sha,
              std::atomic<bool>& cancel,Progress progress={},bool fixture=false);
void AbortNetwork();
std::string Fetch(const std::string& url,std::atomic<bool>& cancel,bool fixture=false);
struct UpdateInfo {json catalog,game,music;bool gameAvailable=false,musicAvailable=false;uint64_t musicBytes=0;int added=0,changed=0,removed=0;};
class Store {
 HANDLE lock_=INVALID_HANDLE_VALUE;
 bool fixtures_=false;
 fs::path saveRoot_;
 void SaveState(const json& next);
 void Journal(const json& record);
 void RequireIdle();
 void BackupSaves(const std::string& transaction);
 void Space(uint64_t bytes);
 void Prune(const json& retired);
public:
 fs::path root;
 json state;
 std::atomic<bool> cancel{false};
 Progress progress;
 uint64_t suppressedBuild=0;
 explicit Store(fs::path directory,bool fixtures=false,fs::path fixtureSaves={});
 ~Store();
 Store(const Store&)=delete;Store& operator=(const Store&)=delete;
 void Recover();
 bool Installed() const;
 fs::path GamePath() const;
 fs::path ManagedMusic() const;
 fs::path PersonalMusic() const;
 fs::path SaveRoot() const{return saveRoot_;}
 UpdateInfo Check(const std::string& catalogURL=CatalogURL);
 void InstallGame(const json& manifest);
 void InstallMusic(const json& manifest);
 void Rollback();
 void Seed(const fs::path& sourceGame,const json& gameManifest,const fs::path& sourceMusic,const json& musicManifest);
 void SetRunning(DWORD pid,const fs::path& exe,const std::string& nonce);
 void ClearRunning(const std::string& nonce);
 void Log(const std::string& message);
};
}
