using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;


namespace WindowsGSM.Plugins
{
    public class TheIsleLegacy : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.TheIsleLegacy", // WindowsGSM.XXXX
            author = "MENIX",
            description = "WindowsGSM plugin for supporting TheIsle Legacy Dedicated Server",
            version = "1.0",
            url = "https://github.com/menix1337/WindowsGSM.TheIsleLegacy", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "412680"; // Game server appId, TheIsle is 412680

        // - Standard Constructor and properties
        public TheIsleLegacy(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public new string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => @"TheIsle\Binaries\Win64\TheIsleServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "The Isle Legacy Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "6777"; // Default port - adjusted from 7777 to 6777 to avoid accidently overlapping with other Unreal Engine Servers by default.
        public string QueryPort = "6000"; //Adjusted to start at 6000 to avoid overlapping in WGSM
        public string Defaultmap = "Isle V3"; // Default map name
        public string Maxplayers = "150"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            string name = String.Concat(FullName.Where(c => !Char.IsWhiteSpace(c)));

            //Download Game.ini
            if (await DownloadGameServerConfig(configPath, configPath))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{session_name}}", _serverData.ServerName);
                File.WriteAllText(configPath, configText);
            }
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            // Check for files in Win64
            string win64 = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Binaries\Win64\"));
            string[] neededFiles = { "steamclient64.dll", "tier0_s64.dll", "vstdlib_s64.dll" };

            foreach (string file in neededFiles)
            {
                if (!File.Exists(Path.Combine(win64, file)))
                {
                    File.Copy(Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), file), Path.Combine(win64, file));
                }
            }

            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Prepare start parameter

            /*Here we wanna update our Game.ini to have the most "Recent Server Name" from the GSM Program - instead of making it just keep the original name from CFG on creation.
            First we of course check the file exists
            - If not existing, we will download a fresh one.
            - If existing, we will update the server name from WGSM into Game.ini
            
             */

            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            if (await adaptGameIniOnLaunch(configPath, configPath))
            {
                //Server Name Values
                string section = "/Script/TheIsle.IGameSession";
                string newServerNameValue = _serverData.ServerName;
                string serverNameKey = "ServerName";

                string[] lines = File.ReadAllLines(configPath);
                bool foundSection = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals("[" + section + "]"))
                    {
                        foundSection = true;
                        continue;
                    }
                    if (foundSection && lines[i].Trim().StartsWith(serverNameKey))
                    {
                        string[] parts = lines[i].Split('=');
                        if (parts.Length >= 2 && !parts[1].Equals(newServerNameValue))
                        {
                            lines[i] = serverNameKey + "=" + newServerNameValue;
                            File.WriteAllLines(configPath, lines);
                            System.Diagnostics.Debug.WriteLine("Value updated in file: " + configPath);
                            break;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Value already exists in file: " + configPath);
                            break;
                        }
                    } else {
                        continue;
                    }
                }
            }


            /*
               Update the Game.ini ServerAdmins= with pre-set adminfiles containing Steam IDS if existing.
               
               Admin List Mode - OBS: NOT REQUIRED
               - Our goal here is to make adding admins on multiple servers easier than having to manually adjust each server every time they change admins

               TWO MODES AVAILABLE: Text-based (txt) and API-based (api)
               
               ============================================
               MODE 1: TEXT-BASED ADMIN LIST (listtype=txt)
               ============================================
               Lets load an admin list from a text file, but only if the adminList=XXX exists in the Param.
               - example: 
               game=Survival;listtype=txt;adminList=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/Adminlist.txt

               Some servers have different admins depending if the server is a Deathmatch type server or not - to help this we can add in a second admin list (adminListTwo) & combine the adminList & adminListTwo
               - example: 
               game=Survival;listtype=txt;adminList=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/Adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/AdminlistDM.txt

               Then the server will merge and update the lists into the game.ini with the IDs of each line in the text files

               adminList.txt file example (Have each id on each line):
               76561197960419839
               76561197960419840
               76561197960419841
               76561197960419842

               then adminListTwo.txt file example:
               76561197960419843

               You can also add more lists like adminListThree.txt, adminListFour.txt
               - just make sure each list starts with adminList

               Will combine into a total of this when put into the game ini:
               ServerAdmins=76561197960419839
               ServerAdmins=76561197960419840
               ServerAdmins=76561197960419841
               ServerAdmins=76561197960419842
               ServerAdmins=76561197960419843

               ============================================
               MODE 2: API-BASED ADMIN LIST (listtype=api)
               ============================================
               Fetches admin list from an API endpoint. Bearer token authentication is optional.
               - example with Bearer token:
               game=Survival;listtype=api;apibearertoken=your_token_here;apiurl=http://your-api-host:port/api/v1/adminlist
               
               - example without Bearer token (if API doesn't require authentication):
               game=Survival;listtype=api;apiurl=http://your-api-host:port/api/v1/adminlist
               
               - example with default filter (only "admin" type):
               game=Survival;listtype=api;apibearertoken=your_token;apiurl=http://your-api-host:port/api/v1/adminlist
               
               - example with include parameter (specify which admin types to include):
               game=Survival;listtype=api;apibearertoken=your_token;apiurl=http://your-api-host:port/api/v1/adminlist;include=admin,trial
               
               - example with include in URL:
               game=Survival;listtype=api;apibearertoken=your_token;apiurl=http://your-api-host:port/api/v1/adminlist?gameType=legacy&include=admin,trial,dmadmin,dmtrial
               
               Note: If apibearertoken is provided, it will be set as "Bearer {token}" in the Authorization header.
               Note: The "include" parameter filters which admin types to fetch. Valid values: admin, trial, dmadmin, dmtrial (comma-separated).
               Note: If "include" is not specified, defaults to "admin" (only full admins).
               Note: You can specify "include" as a parameter (include=admin,trial) or in the URL query string.

               The API endpoint should return JSON in the following format:
               [
                 {
                   "steamId": "76561198000000000"
                 }
               ]

               Note: Only the "steamId" field is extracted and used. All other fields are ignored.

               The system will:
               1. Use the provided apiurl (including any query parameters you specify)
               2. Make GET request (with Bearer token authentication if apibearertoken is provided)
               3. Extract steamId values from response
               4. Update Game.ini with those Steam IDs
               
               Example with query parameters in URL:
               apiurl=http://localhost:3000/api/v1/adminlist?gameType=legacy&serverType=survival
               
               The query parameters in the URL determine what admins are returned from the API.

               Example with all parameters:
               game=Survival;listtype=api;apibearertoken=abc123xyz;apiurl=http://localhost:3000/api/v1/adminlist

               OBS: If admin list is specified, for each time you restart the server it will clear out all admins and re-apply accordingly from the list; 
               - For txt mode: if the source .txt files can be found - otherwise it will keep the original game.ini without refreshing the admins (In case the source is down so you suddenly dont have admin)
               - For api mode: if the API is reachable - otherwise it will keep the original game.ini without refreshing the admins (In case the API is down so you suddenly dont have admin)
            */

            await UpdateAdminList(_serverData.ServerParam, configPath);


            /*
            if server default map contains either Isle V3 or Thenyaw or DV_TestLevel then add the string
            /Game/TheIsle/Maps/Landscape3/Isle_V3 for Isle V3
            /Game/TheIsle/Maps/Thenyaw_Island/Thenyaw_Island for Thenyaw
            /Game/TheIsle/Maps/Developer/DV_TestLevel for Dev Map
            */
            List<string> IsleV3Variations = new List<string>() { "Isle V3", "isle v3", "v3", "islev3" };
            List<string> ThenyawVariations = new List<string>() { "Thenyaw", "thenyaw", "ThenyawIsland", "Thenyaw Island" };
            List<string> TestlevelVariations = new List<string>() { "testlevel", "DV_TestLevel", "dm", "Test Level", "Dev Map", "Dev level" };

            string param = "";
            if (IsleV3Variations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Landscape3/Isle_V3";
            }
            else if (ThenyawVariations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Thenyaw_Island/Thenyaw_Island";
            }
            else if (TestlevelVariations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Developer/DV_TestLevel";
            }
            else
            {
                param = string.Empty;
            }

            //since the ServerStartParam can have multiple things here (such as adminLists) we divide it up using GetGameMode - to make sure we only put the gamemode into the actual Start Param of our game server. GetGameMode() splits out the relevant information to specify gamemode
            string gameMode = await GetGameMode(_serverData.ServerParam);

            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MaxPlayers={_serverData.ServerMaxPlayer}";
            param += $"?{gameMode}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -QueryPort={_serverData.ServerQueryPort}";
            param += $" -nosteamclient -game -server -log";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.CreateNoWindow)
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");

                }
                else
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");
                }
            });
        }

        // Get ini files
        public static async Task<bool> DownloadGameServerConfig(string fileSource, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/TheIsleLegacy/Game.ini", filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
            }

            return File.Exists(filePath);
        }

        public static async Task<bool> adaptGameIniOnLaunch(string fileSource, string filePath)
        {

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            //if the file DOESN'T exist - lets re-create it.
            if (!File.Exists(filePath))
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/TheIsleLegacy/Game.ini", filePath);
                    }
                }
                catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}"); 
                }
            }
            return File.Exists(filePath);
        }

        public static Task<string> GetGameMode(string serverData)
        {
            string defaultGameMode = "game=Survival";
            string[] parts = serverData.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("game=", StringComparison.OrdinalIgnoreCase))
                {
                    if (part.Equals("game=Survival", StringComparison.OrdinalIgnoreCase) ||
                        part.Equals("game=Sandbox", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultGameMode = part;
                        break;
                    }
                }
            }

            return Task.FromResult(defaultGameMode);
        }

        /// <summary>
        /// Updates the admin list in Game.ini based on the configured mode (txt or api).
        /// Supports both text-based file downloads and API-based admin list fetching.
        /// </summary>
        /// <param name="_serverData">Server parameters string containing configuration</param>
        /// <param name="gameIniPath">Path to the Game.ini file to update</param>
        public static async Task UpdateAdminList(string _serverData, string gameIniPath)
        {
            // Parse server parameters into a dictionary for easy access
            // Example: "game=Survival;listtype=txt;adminList=https://..." becomes key-value pairs
            Dictionary<string, string> serverParams = ParseServerParams(_serverData);
            
            // Determine which mode to use: "txt" (text-based) or "api" (API-based)
            // Default to "txt" if not specified for backward compatibility
            string listType = serverParams.ContainsKey("listtype") 
                ? serverParams["listtype"].ToLower() 
                : "txt";

            List<string> combinedAdminList = new List<string>();

            if (listType == "api")
            {
                // ============================================
                // API MODE: Fetch admins from API endpoint
                // ============================================
                // Example params: listtype=api;apibearertoken=abc123;apiurl=http://localhost:3000/api/v1/adminlist
                
                try
                {
                    combinedAdminList = await FetchAdminsFromApi(serverParams);
                }
                catch (Exception ex)
                {
                    // If API fails, log error but don't update admin list (keeps last known list)
                    // This ensures admins remain even if API is temporarily down
                    System.Diagnostics.Debug.WriteLine($"API Admin List Fetch Failed: {ex.Message}");
                    return; // Exit early - don't clear existing admins
                }
            }
            else
            {
                // ============================================
                // TEXT MODE: Download and parse text files
                // ============================================
                // Example params: listtype=txt;adminList=https://...;adminListTwo=https://...
                
                Dictionary<string, string> adminListFiles = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> kvp in serverParams)
                {
                    // Find all parameters starting with "adminList" (adminList, adminListTwo, adminListThree, etc.)
                    if (kvp.Key.StartsWith("adminList", StringComparison.OrdinalIgnoreCase))
                    {
                        adminListFiles.Add(kvp.Key, kvp.Value);
                    }
                }

                // Download and combine all admin list files
                foreach (KeyValuePair<string, string> kvp in adminListFiles)
                {
                    try
                    {
                        using (var client = new WebClient())
                        {
                            // Download text file from URL
                            // Example: https://raw.githubusercontent.com/.../Adminlist.txt
                            string txtFile = await client.DownloadStringTaskAsync(kvp.Value);
                            string[] lines = txtFile.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                            foreach (string line in lines)
                            {
                                string trimmedLine = line.Trim();
                                // Only add non-empty lines (skip blank lines)
                                if (!string.IsNullOrWhiteSpace(trimmedLine))
                                {
                                    combinedAdminList.Add(trimmedLine);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // If one file fails, continue with others
                        // This allows partial success if some files are unavailable
                        continue;
                    }
                }
            }

            // Only update Game.ini if we successfully retrieved at least one admin
            // If list is empty or all sources failed, keep existing admins unchanged
            System.Diagnostics.Debug.WriteLine($"Total admins retrieved: {combinedAdminList.Count}");
            if (combinedAdminList.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Updating Game.ini with {combinedAdminList.Count} admins");
                UpdateGameIniWithAdmins(gameIniPath, combinedAdminList);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No admins retrieved - keeping existing Game.ini unchanged");
            }
        }

        /// <summary>
        /// Parses server parameter string into a dictionary.
        /// Example: "game=Survival;listtype=txt;adminList=https://..." 
        /// Returns: { "game": "Survival", "listtype": "txt", "adminList": "https://..." }
        /// </summary>
        private static Dictionary<string, string> ParseServerParams(string serverData)
        {
            Dictionary<string, string> paramsDict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(serverData))
            {
                return paramsDict;
            }

            string[] parts = serverData.Split(';');
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                string[] keyValue = part.Split(new[] { '=' }, 2);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        paramsDict[key] = value;
                    }
                }
            }

            return paramsDict;
        }

        /// <summary>
        /// Fetches admin list from API endpoint using Bearer token authentication.
        /// Uses the provided API URL as-is, including any query parameters specified by the user.
        /// </summary>
        /// <param name="serverParams">Parsed server parameters dictionary</param>
        /// <returns>List of Steam IDs extracted from API response</returns>
        private static async Task<List<string>> FetchAdminsFromApi(Dictionary<string, string> serverParams)
        {
            List<string> steamIds = new List<string>();

            // Validate required API parameters
            if (!serverParams.ContainsKey("apiurl"))
            {
                throw new Exception("apiurl parameter is required when listtype=api");
            }

            string apiUrl = serverParams["apiurl"].Trim();
            
            // Bearer token is optional - only set if provided
            // Some APIs may not require authentication
            string bearerToken = serverParams.ContainsKey("apibearertoken") 
                ? serverParams["apibearertoken"].Trim() 
                : null;
            
            // Get include parameter for filtering admin types
            // Valid values: admin,trial,dmadmin,dmtrial (comma-separated)
            // If not specified, defaults to "admin" (only full admins)
            // If specified in apiurl, that takes precedence
            string includeFilter = serverParams.ContainsKey("include") 
                ? serverParams["include"].Trim() 
                : null;

            // Build API endpoint URL
            // Handle both cases:
            // 1. Base URL provided: http://localhost:3000 -> append /api/v1/adminlist
            // 2. Full URL with query params provided: http://localhost:3000/api/v1/adminlist?gameType=legacy&serverType=survival -> use as-is
            // The query parameters in the URL determine what admins are returned from the API
            string fullUrl;
            if (apiUrl.IndexOf("/api/v1/adminlist", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Full URL already provided (may include query parameters)
                fullUrl = apiUrl;
                
                // Only add include filter if explicitly provided as a parameter
                // Don't auto-add include=admin if user has already specified query parameters in URL
                if (!string.IsNullOrWhiteSpace(includeFilter) && fullUrl.IndexOf("include=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    string separator = fullUrl.Contains("?") ? "&" : "?";
                    fullUrl += $"{separator}include={includeFilter}";
                }
                // If user provided full URL with query params, respect it and don't auto-add include=admin
                // Only add include=admin if URL has no query parameters at all
                else if (string.IsNullOrWhiteSpace(includeFilter) && !fullUrl.Contains("?") && fullUrl.IndexOf("include=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    // Default to only "admin" type if no include specified AND no query params in URL
                    fullUrl += "?include=admin";
                }
            }
            else
            {
                // Base URL provided, append endpoint path
                fullUrl = apiUrl.TrimEnd('/') + "/api/v1/adminlist";
                
                // Add include filter
                if (!string.IsNullOrWhiteSpace(includeFilter))
                {
                    // Use provided include parameter
                    fullUrl += $"?include={includeFilter}";
                }
                else
                {
                    // Default to only "admin" type if no include specified
                    fullUrl += "?include=admin";
                }
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Fetching admins from API: {fullUrl}");
                
                using (WebClient client = new WebClient())
                {
                    // Set Bearer token in Authorization header if provided
                    // Format: "Bearer YOUR_TOKEN"
                    // Note: apibearertoken is optional - only set header if token is provided
                    if (!string.IsNullOrWhiteSpace(bearerToken))
                    {
                        client.Headers.Add("Authorization", $"Bearer {bearerToken}");
                        System.Diagnostics.Debug.WriteLine("Bearer token added to request");
                    }
                    client.Headers.Add("Content-Type", "application/json");

                    // Make GET request to API endpoint
                    string jsonResponse = await client.DownloadStringTaskAsync(fullUrl);
                    
                    // Debug: Log the response for troubleshooting
                    System.Diagnostics.Debug.WriteLine($"API Response received. Length: {jsonResponse?.Length ?? 0}");
                    if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse.Length > 0)
                    {
                        // Log first 200 characters of response for debugging
                        string preview = jsonResponse.Length > 200 ? jsonResponse.Substring(0, 200) + "..." : jsonResponse;
                        System.Diagnostics.Debug.WriteLine($"API Response preview: {preview}");
                    }

                    // Parse JSON response
                    // Expected format: [ { "steamId": "76561198000000000", "name": "...", "gameType": "...", ... }, ... ]
                    // Note: We only extract "steamId" - all other fields (name, gameType, serverType, adminType, etc.) are ignored
                    // Using regex to extract steamId values (simple and works without external JSON libraries)
                    // Pattern matches: "steamId": "value" or "steamId":"value"
                    if (!string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        MatchCollection matches = Regex.Matches(jsonResponse, @"""steamId""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
                        System.Diagnostics.Debug.WriteLine($"Regex found {matches.Count} matches");
                        
                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count > 1)
                            {
                                string steamId = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(steamId))
                                {
                                    steamIds.Add(steamId);
                                    System.Diagnostics.Debug.WriteLine($"Added Steam ID: {steamId}");
                                }
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Total Steam IDs extracted: {steamIds.Count}");
                }
            }
            catch (WebException webEx)
            {
                // Handle HTTP errors (401 Unauthorized, 500 Server Error, etc.)
                throw new Exception($"API request failed: {webEx.Message}");
            }
            catch (Exception ex)
            {
                // Handle any other errors (network issues, etc.)
                throw new Exception($"API admin list fetch error: {ex.Message}");
            }

            return steamIds;
        }

        /// <summary>
        /// Updates Game.ini file with the provided list of Steam IDs.
        /// Removes all existing ServerAdmins entries and adds new ones.
        /// </summary>
        /// <param name="gameIniPath">Path to Game.ini file</param>
        /// <param name="adminIds">List of Steam IDs to add as admins</param>
        private static void UpdateGameIniWithAdmins(string gameIniPath, List<string> adminIds)
        {
            // Read all lines from Game.ini
            var lines = File.ReadAllLines(gameIniPath).ToList();
            
            // Find the section boundaries
            // Game.ini structure:
            // [/Script/TheIsle.IGameSession]
            // ServerAdmins=...
            // [/script/theisle.igamemode]
            int startIndex = lines.FindIndex(x => x.StartsWith("[/Script/TheIsle.IGameSession]", StringComparison.OrdinalIgnoreCase));
            int endIndex = lines.FindIndex(startIndex, x => x.StartsWith("[/script/theisle.igamemode]", StringComparison.OrdinalIgnoreCase));

            if (startIndex == -1 || endIndex == -1)
            {
                // If sections not found, can't update - log and return
                System.Diagnostics.Debug.WriteLine("Could not find required sections in Game.ini");
                return;
            }

            // Remove all existing ServerAdmins entries
            int currentIndex = startIndex + 1;
            while (currentIndex < endIndex)
            {
                if (lines[currentIndex].StartsWith("ServerAdmins=", StringComparison.OrdinalIgnoreCase))
                {
                    lines.RemoveAt(currentIndex);
                    endIndex--; // Adjust end index since we removed a line
                }
                else
                {
                    currentIndex++;
                }
            }

            // Insert new ServerAdmins entries before the end of the section
            // Format: ServerAdmins=76561198000000000
            int insertIndex = endIndex;
            foreach (string adminId in adminIds)
            {
                // Skip empty or invalid Steam IDs
                if (!string.IsNullOrWhiteSpace(adminId))
                {
                    lines.Insert(insertIndex, $"ServerAdmins={adminId}");
                    insertIndex++;
                }
            }

            // Write updated content back to file
            File.WriteAllLines(gameIniPath, lines);
        }

    }
}