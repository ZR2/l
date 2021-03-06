﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NadekoBot.Extensions;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoStats
    {
        public string BotVersion = "0.8-beta11";

        private static readonly NadekoStats _instance = new NadekoStats();
        public static NadekoStats Instance => _instance;

        private CommandService _service;
        private DiscordClient _client;

        private int _commandsRan = 0;
        private string _statsCache = "";
        private Stopwatch _statsSW = new Stopwatch();

        List<string> messages = new List<string>();

        static NadekoStats() { }

        private NadekoStats() {
            _service = NadekoBot.client.Services.Get<CommandService>();
            _client = NadekoBot.client;

            _statsSW = new Stopwatch();
            _statsSW.Start();
            _service.CommandExecuted += StatsCollector_RanCommand;

            Task.Run(() => StartCollecting());
            Console.WriteLine("Logging enabled.");
        }

        public TimeSpan GetUptime() =>
            DateTime.Now - Process.GetCurrentProcess().StartTime;

        public string GetUptimeString() {
            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            return time.Days + " days, " + time.Hours + " hours, and " + time.Minutes + " minutes.";
        }

        public void LoadStats() {
            _statsCache =
            "`Author: Kwoth`" +
            //$"\nDiscord.Net version: {DiscordConfig.LibVersion}" +
            //$"\nRuntime: {_client.GetRuntime()}" +
            $"\n`Bot Version: {BotVersion}`" +
            //$"\nLogged in as: {_client.CurrentUser.Name}" +
            $"\n`Bot id: {_client.CurrentUser.Id}`" +
            $"\n`Uptime: {GetUptimeString()}`" +
            $"\n`Servers: {_client.Servers.Count()}`" +
            $"\n`Channels: {_client.Servers.Sum(s => s.AllChannels.Count())}`" +
            //$"\nUsers: {_client.Servers.SelectMany(x => x.Users.Select(y => y.Id)).Count()} (non-unique)" +
            $"\n`Heap: {Math.Round((double)GC.GetTotalMemory(true) / 1.MiB(), 2).ToString()} MB`" +
            $"\n`Commands Ran this session: {_commandsRan}`" +
            $"\n`Message queue size:{_client.MessageQueue.Count}`" +
            $"\n`Greeted/Byed {Commands.ServerGreetCommand.Greeted} times.`";
        }

        public string GetStats() {
            if (_statsSW.ElapsedTicks > 5) {
                LoadStats();
                _statsSW.Restart();
            }
            return _statsCache;
        }

        private async Task StartCollecting() {
            while (true) {
                await Task.Delay(new TimeSpan(0, 30, 0));
                try {
                    var onlineUsers = await Task.Run(() => NadekoBot.client.Servers.Sum(x => x.Users.Count()));
                    var realOnlineUsers = await Task.Run(() => NadekoBot.client.Servers
                                                                        .Sum(x => x.Users.Where(u => u.Status == UserStatus.Online).Count()));
                    var connectedServers = NadekoBot.client.Servers.Count();

                    Classes.DBHandler.Instance.InsertData(new Classes._DataModels.Stats {
                        OnlineUsers = onlineUsers,
                        RealOnlineUsers = realOnlineUsers,
                        Uptime = GetUptime(),
                        ConnectedServers = connectedServers,
                        DateAdded = DateTime.Now
                    });
                } catch  {
                    Console.WriteLine("DB Exception in stats collecting.");
                    break;
                }
            }
        }
        //todo - batch save this
        private void StatsCollector_RanCommand(object sender, CommandEventArgs e)
        {
            try {
                _commandsRan++;
                Classes.DBHandler.Instance.InsertData(new Classes._DataModels.Command {
                    ServerId = (long)e.Server.Id,
                    ServerName = e.Server.Name,
                    ChannelId = (long)e.Channel.Id,
                    ChannelName =e.Channel.Name,
                    UserId = (long)e.User.Id,
                    UserName = e.User.Name,
                    CommandName = e.Command.Text,
                    DateAdded = DateTime.Now
                });
            } catch  {
                Console.WriteLine("Parse error in ran command.");
            }
        }
    }
}