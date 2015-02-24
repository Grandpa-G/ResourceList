using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Data;
using System.ComponentModel;
using Terraria;
using TShockAPI;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Threading;
using TerrariaApi.Server;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using System.Net;

namespace ResourceList
{
    [ApiVersion(1, 16)]
    public class ResourceListMain : TerrariaPlugin
    {

        public override string Name
        {
            get { return "ResourceList"; }
        }
        public override string Author
        {
            get { return "by Granpa-G"; }
        }
        public override string Description
        {
            get { return "Provides a list of all TShock Resources"; }
        }
        public override Version Version
        {
            get { return new Version("1.0.2.0"); }
        }
        public ResourceListMain(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            //            ServerApi.Hooks.NetGetData.Register(this, GetData);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnLogin;

            Commands.ChatCommands.Add(new Command("ResourceList.allow", ResourceManager, "resourcelist"));
            Commands.ChatCommands.Add(new Command("ResourceList.allow", ResourceManager, "rl"));


        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //				ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnLogin;
                //GameHooks.Update -= OnUpdate;
                //NetHooks.SendData -= SendData;
            }
            base.Dispose(disposing);
        }
        private static void OnLeave(LeaveEventArgs args)
        {
        }

        private void OnLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
        }

        private void ResourceManager(CommandArgs args)
        {
            bool verbose = false;
            bool current = false;
            string author = "";
            string sortOption = "";

            ResourceListArguments arguments = new ResourceListArguments(args.Parameters.ToArray());
            // foreach(KeyValuePair<string, string> entry in arguments._parsedArguments)
            //{
            //   Console.WriteLine(entry.Key );
            //   Console.WriteLine(entry.Value );
            //}
            if (arguments.Contains("-current") || arguments.Contains("-c"))
                current = true;

            if (arguments.Contains("-verbose") || arguments.Contains("-v"))
                verbose = true;
            if (arguments.Contains("-help"))
            {
                args.Player.SendMessage("Syntax: /resourcelist [-verbose -sort {a|t} -help -author {name}] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage("   -author   Find resource by author", Color.LightSalmon);
                args.Player.SendMessage("   -current  Display currenly loaded plugins", Color.LightSalmon);
                args.Player.SendMessage("   -sort     Sort by a (author), t(title)", Color.LightSalmon);
                args.Player.SendMessage("   -verbose  More information", Color.LightSalmon);
                args.Player.SendMessage("   -help     this information", Color.LightSalmon);
                return;
            }

            int sortIndex = 0;
            sortOption = null;
            if (arguments.Contains("-sort") || arguments.Contains("-s"))
            {
                if (arguments.Contains("-sort"))
                    sortOption = arguments.Sort;
                else
                    sortOption = arguments.SortShort;
                 if (sortOption != null)
                    switch (sortOption[0])
                    {
                        case 'a':
                            sortIndex = 1;
                            break;
                        case 't':
                            sortIndex = 2;
                            break;
                        default:
                            sortIndex = 2;
                            break;
                    }
            }
 
            author = null;
            if (arguments.Contains("-author") || arguments.Contains("-a"))
            {
                if (arguments.Contains("-author"))
                    author = arguments.Author;
                else
                    author = arguments.AuthorShort;
                if (author == null)
                {
                    args.Player.SendMessage("Missing Author", Color.LightSalmon);
                    return;
                }
            }
 
            Resource resources = null;
            if (author == null)
                resources = ListResources("");
            else
                resources = ListResources("&Author=" + author);

            if(resources.error > 0)
            {
                args.Player.SendMessage(String.Format("Error in request: {0}", resources.message), Color.LightSalmon);
                return;
            }

             string format = "";
            string formatc = "";
            switch (sortIndex)
            {
                case 1:     //author
                    resources.Resources.Sort((x, y) => string.Compare(x.AuthorUsername, y.AuthorUsername));
                    formatc = "    {1} {0} {2} Ver:{3}";
                    if (verbose)
                        format = "    {1} {0} Ver:{2} DL:{3}";
                    else
                        format = "    {1} {0} Ver:{2}";
                    break;
                case 0:
                case 2:     //title
                    resources.Resources.Sort((x, y) => string.Compare(x.Title, y.Title));
                    formatc = "    {0} {1} {2} Ver:{3}";
                    if (verbose)
                        format = "    {0} [{1}] Ver:{2} DL:{3}";
                    else
                        format = "    {0} {1} Ver:{2}";
                    break;
                default:
                    resources.Resources.Sort((x, y) => string.Compare(x.Title, y.Title));
                    formatc = "    {0} {1} {2} Ver:{3}";
                    if (verbose)
                        format = "    {0} [{1}] Ver:{2} DL:{3}";
                    else
                        format = "    {0} {1} Ver:{2}";
                    break;
            }

            if (current)
            {
                args.Player.SendMessage(String.Format("Current Plugins ({0})", ServerApi.Plugins.Count), Color.LightSalmon);
                for (int i = 0; i < ServerApi.Plugins.Count; i++)
                {
                    PluginContainer pc = ServerApi.Plugins.ElementAt(i);
                    args.Player.SendMessage(String.Format(formatc, pc.Plugin.Name, pc.Plugin.Description, pc.Plugin.Author, pc.Plugin.Version), Color.LightSalmon);
                }
            }
 
            args.Player.SendMessage(String.Format("Resources Available ({0})", resources.Count), Color.LightSalmon);
            foreach (ResourceList rl in resources.Resources)
            {
                args.Player.SendMessage(String.Format(format, rl.Title, rl.AuthorUsername, rl.VersionString, rl.TimesDownloaded), Color.Black);
            }
        }

        private static string GetHTTP(string url)
        {
            var client = (HttpWebRequest)WebRequest.Create(url);
            client.Timeout = 50000;
            try
            {
                using (var resp = TShock.Utils.GetResponseNoException(client))
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        //                        throw new IOException("Server did not respond with an OK.");
                    }

                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        string updatejson = reader.ReadToEnd();
                        return updatejson;
                    }
                }
            }
            catch (NullReferenceException) { }
            return "";
        }

        private static Resource ListResources(string options)
        {
            string url = String.Format("http://tshock.co/xf/api.php?action=getresources{0}", options);
 
            string json = GetHTTP(url);
 
            var r = JsonConvert.DeserializeObject<Resource>(json);

            return (Resource)r;
        }

    }
    #region application specific commands
    public class ResourceListArguments : InputArguments
    {
        public string CountLimit
        {
            get { return GetValue("limit"); }
        }

        public string Current
        {
            get { return GetValue("-current"); }
        }
        public string CurrentShort
        {
            get { return GetValue("-c"); }
        }

        public string Verbose
        {
            get { return GetValue("-verbose"); }
        }
        public string VerboseShort
        {
            get { return GetValue("-v"); }
        }

        public string Help
        {
            get { return GetValue("-help"); }
        }

        public string Sort
        {
            get { return GetValue("-sort"); }
        }
        public string SortShort
        {
            get { return GetValue("-s"); }
        }
        public string Author
        {
            get { return GetValue("-author"); }
        }
        public string AuthorShort
        {
            get { return GetValue("-a"); }
        }

        public ResourceListArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion
    #region Class defintion
    // Generated by Xamasoft JSON Class Generator
    // http://www.xamasoft.com/json-class-generator

    internal class ResourceList
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author_id")]
        public int AuthorId { get; set; }

        [JsonProperty("author_username")]
        public string AuthorUsername { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("creation_date")]
        public int CreationDate { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("version_id")]
        public int VersionId { get; set; }

        [JsonProperty("version_string")]
        public string VersionString { get; set; }

        [JsonProperty("file_hash")]
        public object FileHash { get; set; }

        [JsonProperty("description_id")]
        public int DescriptionId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("thread_id")]
        public int ThreadId { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("times_downloaded")]
        public int TimesDownloaded { get; set; }

        [JsonProperty("times_rated")]
        public int TimesRated { get; set; }

        [JsonProperty("rating_sum")]
        public int RatingSum { get; set; }

        [JsonProperty("rating_avg")]
        public int RatingAvg { get; set; }

        [JsonProperty("rating_weighted")]
        public int RatingWeighted { get; set; }

        [JsonProperty("times_updated")]
        public int TimesUpdated { get; set; }

        [JsonProperty("times_reviewed")]
        public int TimesReviewed { get; set; }

        [JsonProperty("last_update")]
        public int LastUpdate { get; set; }

        [JsonProperty("custom_fields")]
        public string CustomFields { get; set; }
    }

    internal class Resource
    {

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("error")]
        public int error { get; set; }

        [JsonProperty("message")]
        public string message { get; set; }

        [JsonProperty("resources")]
        public List<ResourceList> Resources { get; set; }
    }
    #endregion
}
