﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Raven.Json.Linq;
using Raven.Server;

namespace Raven.VisualHost
{
	public partial class ServerLog : UserControl
	{
		public int NumOfRequests;

		public ServerLog()
		{
			InitializeComponent();
		}

		public RavenDbServer Server { get; set; }

		public string Url { get; set; }

		public void AddRequest(TrackedRequest request)
		{
			IAsyncResult a = null;
			a = BeginInvoke((Action)(() =>
			{
				var asyncResult = a;
				if (asyncResult != null)
				{
					EndInvoke(asyncResult);
				}
				RequestsLists.Items.Add(new ListViewItem(new string[] { request.Method, request.Status.ToString(), request.Url })
				{
					Tag = request
				});
			}));
		}

		private void RequestsLists_SelectedIndexChanged(object sender, EventArgs e)
		{
			Reset();
			if (RequestsLists.SelectedItems.Count == 0)
			{
				return;
			}
			var trackedRequest = ((TrackedRequest)RequestsLists.SelectedItems[0].Tag);

			RequestText.Text = GetRequestText(trackedRequest);
			ResponseText.Text = GetResponseText(trackedRequest);
		}

		private string GetRequestText(TrackedRequest trackedRequest)
		{
			var requestStringBuilder = new StringBuilder();
			//AppendHeaders(trackedRequest.RequestHeaders, requestStringBuilder);

			requestStringBuilder.AppendLine();
			WriteStreamContentMaybeJson(requestStringBuilder, trackedRequest.RequestContent, trackedRequest.RequestHeaders);

			return requestStringBuilder.ToString();
		}

		private string GetResponseText(TrackedRequest trackedRequest)
		{
			var requestStringBuilder = new StringBuilder();
			//AppendHeaders(trackedRequest.ResponseHeaders, requestStringBuilder);

			requestStringBuilder.AppendLine();

			WriteStreamContentMaybeJson(requestStringBuilder, trackedRequest.ResponseContent, trackedRequest.ResponseHeaders);

			return requestStringBuilder.ToString();
		}

		private void WriteStreamContentMaybeJson(StringBuilder requestStringBuilder, Stream stream, NameValueCollection headers)
		{
			stream.Position = 0;

			if(headers["Content-Type"] == "application/json; charset=utf-8")
			{
				var t = RavenJToken.Load(new JsonTextReader(new StreamReader(stream)));
				requestStringBuilder.Append(t.ToString(Formatting.Indented));
			}
			else
			{
				var streamReader = new StreamReader(stream);
				requestStringBuilder.Append(streamReader.ReadToEnd());	
			}
		}

		private void AppendHeaders(NameValueCollection nameValueCollection, StringBuilder sb)
		{
			foreach (string requestHeader in nameValueCollection)
			{
				var values = nameValueCollection.GetValues(requestHeader);
				if (values == null)
					continue;
				foreach (var value in values)
				{
					sb.Append(requestHeader).Append(": ").Append(value).AppendLine();
				}
			}
		}

		private void Reset()
		{
			ResponseText.Text = string.Empty;
			RequestText.Text = string.Empty;
		}

		public void Clear()
		{
			Reset();
			RequestsLists.Items.Clear();
			NumOfRequests = 0;
		}
	}
}