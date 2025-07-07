using System;
using System.Collections.Generic;
using Supabase.Postgrest;

namespace PostgrestTests
{
	internal static class Helpers
	{
		internal static Client GetHostedClient()
		{
			var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
			var publicKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");

			var client = new Client($"{url}/rest/v1", new ClientOptions
			{
				Headers = new Dictionary<string, string>
				{
					{"apikey", publicKey! }
				}
			});

			return client;
		}
		
		internal static Client GetLocalClient()
		{
			var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
			if (url == null) url = "http://localhost:54321";
			var publicKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLIC_KEY");
			if (publicKey == null) publicKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

			var client = new Client($"{url}/rest/v1", new ClientOptions
			{
				Headers = new Dictionary<string, string>
				{
					{"apikey", publicKey! }
				}
			});

			return client;
		}
	}
}
