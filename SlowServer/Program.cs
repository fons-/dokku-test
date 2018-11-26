using Nancy;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlowServer
{
	class Program
	{
		const string StagingPort = "8080";
		static readonly string HOST = Environment.GetEnvironmentVariable("HOST");
		static readonly string PORT = Environment.GetEnvironmentVariable("PORT");

		enum Env { Staging, Deployment }

		static Env CurrentEnv
		{
			get
			{
				return PORT == null ? Env.Staging : Env.Deployment;
			}
		}


		static Uri CurrentAddress
		{
			get
			{
				switch(CurrentEnv)
				{
					case Env.Staging:
						return new Uri("http://localhost:" + StagingPort);
					case Env.Deployment:
						var host = string.IsNullOrEmpty(HOST) ? "localhost" : HOST;
						return new Uri("http://" + host + ":" + PORT);
					default:
						throw new Exception("Unexpected environment");
				}
			}
		}

		static void Main(string[] args)
		{
			var config = new HostConfiguration() { UrlReservations = new UrlReservations { CreateAutomatically = true } };
			using(var host = new Nancy.Hosting.Self.NancyHost(config, CurrentAddress))
			{
				host.Start();
				Console.WriteLine("Running. Press any key to stop.");
				Console.ReadKey();
			}
		}
	}

	public static class Queue
	{
		public static bool visited = false;
		private static string name;
		public static string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
				visited = true;
				tcs?.TrySetResult(true);
			}
		}
		public static TaskCompletionSource<bool> tcs;
	}

	public class Module : Nancy.NancyModule
	{
		public static int i = 0;
		public Module()
		{
			Before += ctx =>
			{
				Console.WriteLine(ctx.Request.Url.HostName);
				if(ctx.Request.Url.HostName == "api.printi.me")
				{
					Console.WriteLine("changed path");
					ctx.Request.Url.Path = "/api" + ctx.Request.Url.Path;
				}
				return null;
			};

			Get["/"] = _ => "hoi";
			
			Get["/set"] = _ =>
			{

				Queue.Name = "hoppa";
				return "ok";
			};

			Get["/get", true] = async (ctx, ct) =>
			{

				Queue.tcs?.TrySetResult(false);
				i++;
				Console.WriteLine("jop");
				if(Queue.visited)
				{
					return Queue.Name;
				}
				Queue.tcs = new TaskCompletionSource<bool>();
				await Task.WhenAny(Queue.tcs.Task, Task.Delay(2000));
				if(Queue.tcs.Task.IsCompleted)
				{
					return Queue.Name;
				}
				return "helaas pindakaas " + i;
			};
		}
	}
}
