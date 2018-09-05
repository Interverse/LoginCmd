using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace LoginCmd {
    [ApiVersion(2, 1)]
    public class LoginCmd : TerrariaPlugin {
        public override string Name => "LoginCmd";
        public override string Author => "Johuan";
        public override string Description => "Allows execution of commands on login";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public LoginCmd(Main game) : base(game) {
        }

        public override void Initialize() {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            Database.DBConnect();
            PlayerHooks.PlayerPostLogin += OnPostLogin;
        }

        protected override void Dispose(bool Disposing) {
            if (Disposing) {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                PlayerHooks.PlayerPostLogin -= OnPostLogin;
            }
            base.Dispose(Disposing);
        }

        private void OnInitialize(EventArgs args) {
            Commands.ChatCommands.Add(new Command("logincmd", LoginCommand, "logincmd"));
        }

        private void LoginCommand(CommandArgs args) {
            if (!args.Parameters.Any()) {
                args.Player.SendErrorMessage("Invalid syntax. /logincmd <add/del/list/delall> <command>");
                return;
            }
            switch (args.Parameters[0]) {
                case "add":
                    args.Parameters.RemoveAt(0);
                    string text = string.Join(" ", args.Parameters);
                    if (!text.StartsWith("/"))
                        text = "/" + text;
                    Database.AddCMD(args.Player.User.ID, text);
                    args.Player.SendSuccessMessage("Added " + text + " to login command list");
                    break;
                case "del":
                    args.Parameters.RemoveAt(0);
                    string input = "/" + string.Join(" ", args.Parameters);
                    if (Database.DelCMD(args.Player.User.ID, input)) {
                        args.Player.SendSuccessMessage("Deleted " + input + " from login command list");
                    } else {
                        args.Player.SendErrorMessage("Failed to delete " + input + " from login command list");
                    }
                    break;
                case "list":
                    args.Player.SendSuccessMessage("Commands: " + Database.ListCMD(args.Player.User.ID));
                    break;
                case "delall":
                    Database.DelAllCMD(args.Player.User.ID);
                    args.Player.SendSuccessMessage("Deleted all login commands associated with this account");
                    break;
                default:
                    args.Player.SendErrorMessage("Invalid syntax. /logincmd <add/del/list/delall> <command>");
                    break;
            }
        }

        private void OnPostLogin(PlayerPostLoginEventArgs e) {
            Database.LoginCommands(e.Player);
        }
    }
}
