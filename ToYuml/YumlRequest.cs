using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.IO;

namespace ToYuml
{
	public class YumlRequest
	{
		const string YUML_REQUEST_URI = "http://yuml.me/diagram/{0}/class/";
		const string YUML_IMG_URI = "http://yuml.me/{0}";

		//
		// request an image from the yuml.me API
		// HTTP POST
		// dsl_text = yuml string
		//
		public static void Request(string yuml, bool plain, string outFileName)
		{
			using (var wb = new WebClient()) {
				// POST the yuml. An image path is returned
				var data = new NameValueCollection();
				data["dsl_text"] = yuml;
				string url = string.Format(YUML_REQUEST_URI, plain ? "plain" : "scruffy");
				byte[] response = wb.UploadValues(url, "POST", data);
				string img_path = Encoding.ASCII.GetString(response);
				if (!img_path.EndsWith(".png")) {
					throw new InvalidOperationException("Path to image was not received: " + img_path);
				}

				// if it's a valid image filename, download the image
				string img_url = string.Format(YUML_IMG_URI, img_path);
				byte[] img = wb.DownloadData(img_url);
				using (var bw= new BinaryWriter(new FileStream(outFileName, FileMode.Create))) {
					bw.Write(img);
				}
			}
		}
	}
}
