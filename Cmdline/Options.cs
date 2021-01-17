using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using log4net;
using log4net.Core;
using CommandLine;
using CommandLine.Text;

namespace CKAN.CmdLine
{
    // Look, parsing options is so easy and beautiful I made
    // it into a special class for you to admire!

    public class Options
    {
        public string action  { get; set; }
        public object options { get; set; }

        /// <summary>
        /// Returns an options object on success. Prints a default help
        /// screen and throws a BadCommandKraken on failure.
        /// </summary>
        public Options(string[] args)
        {
            Parser.Default.ParseArgumentsStrict
            (
                args, new Actions(), (verb, suboptions) =>
                {
                    action  = verb;
                    options = suboptions;
                },
                delegate
                {
                    throw new BadCommandKraken();
                }
            );
        }
    }

    // Actions supported by our client go here.

    internal class Actions : VerbCommandOptions
    {
        [VerbOption("gui", HelpText = "Start the CKAN GUI")]
        public GuiOptions GuiOptions { get; set; }

        [VerbOption("consoleui", HelpText = "Start the CKAN console UI")]
        public ConsoleUIOptions ConsoleUIOptions { get; set; }

        [VerbOption("prompt", HelpText = "Run CKAN prompt for executing multiple commands in a row")]
        public CommonOptions PromptOptions { get; set; }

        [VerbOption("search", HelpText = "Search for mods")]
        public SearchOptions SearchOptions { get; set; }

        [VerbOption("upgrade", HelpText = "Upgrade an installed mod")]
        public UpgradeOptions Upgrade { get; set; }

        [VerbOption("update", HelpText = "Update list of available mods")]
        public UpdateOptions Update { get; set; }

        [VerbOption("available", HelpText = "List available mods")]
        public AvailableOptions Available { get; set; }

        [VerbOption("install", HelpText = "Install a mod")]
        public InstallOptions Install { get; set; }

        [VerbOption("remove", HelpText = "Remove an installed mod")]
        public RemoveOptions Remove { get; set; }

        [VerbOption("import", HelpText = "Import manually downloaded mods")]
        public ImportOptions Import { get; set; }

        [VerbOption("scan", HelpText = "Scan for manually installed mods")]
        public ScanOptions Scan { get; set; }

        [VerbOption("list", HelpText = "List installed modules")]
        public ListOptions List { get; set; }

        [VerbOption("show", HelpText = "Show information about a mod")]
        public ShowOptions Show { get; set; }

        [VerbOption("clean", HelpText = "Clean away downloaded files from the cache")]
        public CleanOptions Clean { get; set; }

        [VerbOption("repair", HelpText = "Attempt various automatic repairs")]
        public SubCommandOptions Repair { get; set; }

        [VerbOption("replace", HelpText = "Replace list of replaceable mods")]
        public ReplaceOptions Replace { get; set; }

        [VerbOption("repo", HelpText = "Manage CKAN repositories")]
        public SubCommandOptions Repo { get; set; }

        [VerbOption("mark", HelpText = "Edit flags on modules")]
        public SubCommandOptions Mark { get; set; }

        [VerbOption("instance", HelpText = "Manage game instances")]
        public SubCommandOptions Instance { get; set; }

        [VerbOption("authtoken", HelpText = "Manage authentication tokens")]
        public AuthTokenSubOptions AuthToken { get; set; }

        [VerbOption("cache", HelpText = "Manage download cache path")]
        public SubCommandOptions Cache { get; set; }

        [VerbOption("compat", HelpText = "Manage game version compatibility")]
        public SubCommandOptions Compat { get; set; }

        [VerbOption("compare", HelpText = "Compare version strings")]
        public CompareOptions Compare { get; set; }

        [VerbOption("version", HelpText = "Show the version of the CKAN client being used")]
        public VersionOptions Version { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            HelpText ht = HelpText.AutoBuild(this, verb);

            // Add a usage prefix line
            ht.AddPreOptionsLine(" ");
            if (string.IsNullOrEmpty(verb))
            {
                ht.AddPreOptionsLine($"Usage: ckan <command> [options]");
            }
            else
            {
                ht.AddPreOptionsLine(verb + " - " + GetDescription(verb));
                switch (verb)
                {
                    // First the commands that deal with mods
                    case "add":
                    case "install":
                    case "remove":
                    case "uninstall":
                    case "upgrade":
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options] modules");
                        break;
                    case "show":
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options] module");
                        break;

                    // Now the commands with other string arguments
                    case "search":
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options] substring");
                        break;
                    case "compare":
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options] version1 version2");
                        break;
                    case "import":
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options] paths");
                        break;

                    // Now the commands with only --flag type options
                    case "gui":
                    case "available":
                    case "list":
                    case "update":
                    case "scan":
                    case "clean":
                    case "version":
                    default:
                        ht.AddPreOptionsLine($"Usage: ckan {verb} [options]");
                        break;
                }
            }
            return ht;
        }

    }

    public abstract class VerbCommandOptions
    {
        protected string GetDescription(string verb)
        {
            var info = this.GetType().GetProperties();
            foreach (var property in info)
            {
                BaseOptionAttribute attrib = (BaseOptionAttribute)Attribute.GetCustomAttribute(
                    property, typeof(BaseOptionAttribute), false);
                if (attrib != null && attrib.LongName == verb)
                    return attrib.HelpText;
            }
            return "";
        }
    }

    // Options common to all classes.

    public class CommonOptions
    {
        [Option('v', "verbose", DefaultValue = false, HelpText = "Show more of what's going on when running.")]
        public bool Verbose { get; set; }

        [Option('d', "debug", DefaultValue = false, HelpText = "Show debugging level messages. Implies verbose")]
        public bool Debug { get; set; }

        [Option("debugger", DefaultValue = false, HelpText = "Launch debugger at start")]
        public bool Debugger { get; set; }

        [Option("net-useragent", HelpText = "Set the default user-agent string for HTTP requests")]
        public string NetUserAgent { get; set; }

        [Option("headless", DefaultValue = false, HelpText = "Set to disable all prompts")]
        public bool Headless { get; set; }

        [Option("asroot", DefaultValue = false, HelpText = "Allows CKAN to run as root on Linux-based systems")]
        public bool AsRoot { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }

        public virtual int Handle(GameInstanceManager manager, IUser user)
        {
            CheckMonoVersion(user, 3, 1, 0);

            // Processes in Docker containers normally run as root.
            // If we are running in a Docker container, do not require --asroot.
            // Docker creates a .dockerenv file in the root of each container.
            if ((Platform.IsUnix || Platform.IsMac) && CmdLineUtil.GetUID() == 0 && !File.Exists("/.dockerenv"))
            {
                if (!AsRoot)
                {
                    user.RaiseError("You are trying to run CKAN as root.\r\nThis is a bad idea and there is absolutely no good reason to do it. Please run CKAN from a user account (or use --asroot if you are feeling brave).");
                    return Exit.ERROR;
                }
                else
                {
                    user.RaiseMessage("Warning: Running CKAN as root!");
                }
            }

            if (Debug)
            {
                LogManager.GetRepository().Threshold = Level.Debug;
                log.Info("Debug logging enabled");
            }
            else if (Verbose)
            {
                LogManager.GetRepository().Threshold = Level.Info;
                log.Info("Verbose logging enabled");
            }

            // Assign user-agent string if user has given us one
            if (NetUserAgent != null)
            {
                Net.UserAgentString = NetUserAgent;
            }

            return Exit.OK;
        }

        /// <summary>
        /// Combine two options objects.
        /// This is mainly to ensure that --headless carries through for prompt.
        /// </summary>
        /// <param name="otherOpts">Options object to merge into this one</param>
        public void Merge(CommonOptions otherOpts)
        {
            if (otherOpts != null)
            {
                Verbose      = Verbose      || otherOpts.Verbose;
                Debug        = Debug        || otherOpts.Debug;
                Debugger     = Debugger     || otherOpts.Debugger;
                NetUserAgent = NetUserAgent ?? otherOpts.NetUserAgent;
                Headless     = Headless     || otherOpts.Headless;
                AsRoot       = AsRoot       || otherOpts.AsRoot;
            }
        }

        private static void CheckMonoVersion(IUser user, int rec_major, int rec_minor, int rec_patch)
        {
            try
            {
                Type type = Type.GetType("Mono.Runtime");
                if (type == null) return;

                MethodInfo display_name = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (display_name != null)
                {
                    var version_string = (string) display_name.Invoke(null, null);
                    var match = Regex.Match(version_string, @"^\D*(?<major>[\d]+)\.(?<minor>\d+)\.(?<revision>\d+).*$");

                    if (match.Success)
                    {
                        int major = Int32.Parse(match.Groups["major"].Value);
                        int minor = Int32.Parse(match.Groups["minor"].Value);
                        int patch = Int32.Parse(match.Groups["revision"].Value);

                        if (major < rec_major || (major == rec_major && minor < rec_minor))
                        {
                            user.RaiseMessage(
                                "Warning. Detected mono runtime of {0} is less than the recommended version of {1}\r\n",
                                String.Join(".", major, minor, patch),
                                String.Join(".", rec_major, rec_minor, rec_patch)
                                );
                            user.RaiseMessage("Update recommend\r\n");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignored. This may be fragile and is just a warning method
            }
        }

        protected static readonly ILog log = LogManager.GetLogger(typeof(CommonOptions));
    }

    public class InstanceSpecificOptions : CommonOptions
    {
        [Option("instance", HelpText = "Game instance to use")]
        public string Instance { get; set; }

        [Option("gamedir", HelpText = "Game dir to use")]
        public string Gamedir { get; set; }

        public override int Handle(GameInstanceManager manager, IUser user)
        {
            int exitCode = base.Handle(manager, user);
            if (exitCode == Exit.OK)
            {
                // User provided game instance
                if (Gamedir != null && Instance != null)
                {
                    user.RaiseMessage("--instance and --gamedir can't be specified at the same time");
                    return Exit.BADOPT;
                }

                try
                {
                    if (!string.IsNullOrEmpty(Instance))
                    {
                        // Set a game directory by its alias.
                        manager.SetCurrentInstance(Instance);
                    }
                    else if (!string.IsNullOrEmpty(Gamedir))
                    {
                        // Set a game directory by its path
                        manager.SetCurrentInstanceByPath(Gamedir);
                    }
                }
                catch (NotKSPDirKraken k)
                {
                    user.RaiseMessage("Sorry, {0} does not appear to be a game instance", k.path);
                    return Exit.BADOPT;
                }
                catch (InvalidKSPInstanceKraken k)
                {
                    user.RaiseMessage("Invalid game instance specified \"{0}\", use '--gamedir' to specify by path, or 'instance list' to see known game instances", k.instance);
                    return Exit.BADOPT;
                }
            }
            return exitCode;
        }
    }

    /// <summary>
    /// For things which are subcommands ('instance', 'repair' etc), we just grab a list
    /// we can pass on.
    /// </summary>
    public class SubCommandOptions : CommonOptions
    {
        [ValueList(typeof(List<string>))]
        public List<string> options { get; set; }

        public SubCommandOptions() { }

        public SubCommandOptions(string[] args)
        {
            options = new List<string>(args).GetRange(1, args.Length - 1);
        }
    }

    // Each action defines its own options that it supports.
    // Don't forget to cast to this type when you're processing them later on.

    internal class InstallOptions : InstanceSpecificOptions
    {
        [OptionArray('c', "ckanfiles", HelpText = "Local CKAN files to process")]
        public string[] ckan_files { get; set; }

        [Option("no-recommends", DefaultValue = false, HelpText = "Do not install recommended modules")]
        public bool no_recommends { get; set; }

        [Option("with-suggests", DefaultValue = false, HelpText = "Install suggested modules")]
        public bool with_suggests { get; set; }

        [Option("with-all-suggests", DefaultValue = false, HelpText = "Install suggested modules all the way down")]
        public bool with_all_suggests { get; set; }

        [Option("allow-incompatible", DefaultValue = false, HelpText = "Install modules that are not compatible with the current game version")]
        public bool allow_incompatible { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> modules { get; set; }
    }

    internal class UpgradeOptions : InstanceSpecificOptions
    {
        [Option('c', "ckanfile", HelpText = "Local CKAN file to process")]
        public string ckan_file { get; set; }

        [Option("no-recommends", DefaultValue = false, HelpText = "Do not install recommended modules")]
        public bool no_recommends { get; set; }

        [Option("with-suggests", DefaultValue = false, HelpText = "Install suggested modules")]
        public bool with_suggests { get; set; }

        [Option("with-all-suggests", DefaultValue = false, HelpText = "Install suggested modules all the way down")]
        public bool with_all_suggests { get; set; }

        [Option("all", DefaultValue = false, HelpText = "Upgrade all available updated modules")]
        public bool upgrade_all { get; set; }

        [ValueList(typeof (List<string>))]
        public List<string> modules { get; set; }
    }

    internal class ReplaceOptions : InstanceSpecificOptions
    {
        [Option('c', "ckanfile", HelpText = "Local CKAN file to process")]
        public string ckan_file { get; set; }

        [Option("no-recommends", HelpText = "Do not install recommended modules")]
        public bool no_recommends { get; set; }

        [Option("with-suggests", HelpText = "Install suggested modules")]
        public bool with_suggests { get; set; }

        [Option("with-all-suggests", HelpText = "Install suggested modules all the way down")]
        public bool with_all_suggests { get; set; }

        [Option("allow-incompatible", DefaultValue = false, HelpText = "Install modules that are not compatible with the current game version")]
        public bool allow_incompatible { get; set; }

        [Option("all", HelpText = "Replace all available replaced modules")]
        public bool replace_all { get; set; }

        // TODO: How do we provide helptext on this?
        [ValueList(typeof (List<string>))]
        public List<string> modules { get; set; }
    }

    internal class ScanOptions : InstanceSpecificOptions
    {
    }

    internal class ListOptions : InstanceSpecificOptions
    {
        [Option("porcelain", HelpText = "Dump raw list of modules, good for shell scripting")]
        public bool porcelain { get; set; }

        [Option("export", HelpText = "Export list of modules in specified format to stdout")]
        public string export { get; set; }
    }

    internal class VersionOptions   : CommonOptions { }
    internal class CleanOptions     : InstanceSpecificOptions { }

    internal class AvailableOptions : InstanceSpecificOptions
    {
        [Option("detail", HelpText = "Show short description of each module")]
        public bool detail { get; set; }
    }

    internal class GuiOptions : InstanceSpecificOptions
    {
        [Option("show-console", HelpText = "Shows the console while running the GUI")]
        public bool ShowConsole { get; set; }
    }

    internal class ConsoleUIOptions : InstanceSpecificOptions { }

    internal class UpdateOptions : InstanceSpecificOptions
    {
        // This option is really meant for devs testing their CKAN-meta forks.
        [Option('r', "repo", HelpText = "CKAN repository to use (experimental!)")]
        public string repo { get; set; }

        [Option("all", DefaultValue = false, HelpText = "Upgrade all available updated modules")]
        public bool update_all { get; set; }

        [Option("list-changes", DefaultValue = false, HelpText = "List new and removed modules")]
        public bool list_changes { get; set; }
    }

    internal class RemoveOptions : InstanceSpecificOptions
    {
        [Option("re", HelpText = "Parse arguments as regular expressions")]
        public bool regex { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> modules { get; set; }

        [Option("all", DefaultValue = false, HelpText = "Remove all installed mods.")]
        public bool rmall { get; set; }
    }

    internal class ImportOptions : InstanceSpecificOptions
    {
        [ValueList(typeof(List<string>))]
        public List<string> paths { get; set; }
    }

    internal class ShowOptions : InstanceSpecificOptions
    {
        [ValueOption(0)] public string Modname { get; set; }
    }

    internal class SearchOptions : InstanceSpecificOptions
    {
        [Option("detail", HelpText = "Show full name, latest compatible version and short description of each module")]
        public bool detail { get; set; }

        [Option("all", HelpText = "Show incompatible mods too")]
        public bool all { get; set; }

        [Option("author", HelpText = "Limit search results to mods by matching authors")]
        public string author_term { get; set; }

        [ValueOption(0)]
        public string search_term { get; set; }
    }

    internal class CompareOptions : CommonOptions
    {
        [Option("machine-readable", HelpText = "Output in a machine readable format: -1, 0 or 1")]
        public bool machine_readable { get; set;}

        [ValueOption(0)] public string Left  { get; set; }
        [ValueOption(1)] public string Right { get; set; }
    }
}
