using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Net;

namespace Wasm2CIL {
	public class WebassemblyEmbedder
	{
		Parser parser;
		Type emitted;

		public WebassemblyEmbedder (Stream reader)
		{
			parser = new Parser (reader);
		
		}

		public static WebassemblyEmbedder
		FromUrl (string URL)
		{
			using (var source = WebRequest.Create(URL).GetResponse().GetResponseStream ()) {
				return new WebassemblyEmbedder (source);
			}
		}

		public static WebassemblyEmbedder
		FromFile (string inputPath)
		{
			using (var source = File.Open (inputPath, FileMode.Open)) {
				return new WebassemblyEmbedder (source);
			}
		}

		// The emitted code subclasses this runtime
		public Type
		Load (string name)
		{
			if (this.emitted == null)
				this.emitted = this.parser.Emit (name, null);

			return emitted;
		}

		public void 
		Save (string name, string path)
		{
			this.parser.Emit (name, path);
		}

		//public void Run (string functionName, object [] args)
		//{
			//if (!this.emitted)
				//throw new Exception ("Must first Load () WASM Assembly with provided name");
				//
				// var instance = 

			//lock (instance) {
			//var method = this.emitted.GetMethod (functionName);
			//var ret = method.Invoke (instance, args);
			//Console.WriteLine ("Function {0} ({1}) returned {2}", functionName, args, ret.ToString ());
			//}
		//}

		public static void Main (string [] args) 
		{
			var inputPath = args [0];
			var outputName = args [1];
			var outputPath = String.Format ("{0}.dll", outputName);

			var driver = WebassemblyEmbedder.FromFile (inputPath);
			driver.Save (outputName, outputPath);
      //Console.ReadKey();
		}

	}
}
