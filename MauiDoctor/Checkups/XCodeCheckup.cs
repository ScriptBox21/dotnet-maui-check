﻿using System;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class XCodeCheckup : Checkup
	{
		public XCodeCheckup(string minimumVersion, string exactVersion = null)
			: this(NuGetVersion.Parse(minimumVersion), string.IsNullOrEmpty(exactVersion) ? null : NuGetVersion.Parse(exactVersion))
		{
		}

		public XCodeCheckup(SemanticVersion minimumVersion, SemanticVersion exactVersion = null)
		{
			ExactVersion = exactVersion;
			MinimumVersion = minimumVersion;
		}

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public SemanticVersion MinimumVersion { get; private set; } = NuGetVersion.Parse("12.3");

		public SemanticVersion ExactVersion { get; private set; }

		public override string Id => "xcode";

		public override string Title => $"XCode {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override async Task<Diagonosis> Examine()
		{
			var info = await GetInfo();

			if (NuGetVersion.TryParse(info.Version?.ToString(), out var semVer))
			{
				if (semVer.IsCompatible(MinimumVersion, ExactVersion))
				{
					ReportStatus($"XCode.app ({info.Version} {info.Build})", Status.Ok);
					return Diagonosis.Ok(this);
				}
			}

			ReportStatus($"XCode.app ({info.Version}) not found.", Status.Error);

			return new Diagonosis(Status.Error, this, new Prescription($"Download XCode {MinimumVersion.ThisOrExact(ExactVersion)}"));
		}

		Task<XCodeInfo> GetInfo()
		{
			//Xcode 12.4
			//Build version 12D4e
			var r = ShellProcessRunner.Run("xcodebuild", "-version");

			var info = new XCodeInfo();

			foreach (var line in r.StandardOutput)
			{
				if (line.StartsWith("Xcode"))
				{
					var vstr = line.Substring(5).Trim();
					if (Version.TryParse(vstr, out var v))
						info.Version = v;
				}
				else if (line.StartsWith("Build version"))
				{
					info.Build = line.Substring(13)?.Trim();
				}
			}

			return Task.FromResult(info);
		}
	}

	public struct XCodeInfo
	{
		public Version Version;
		public string Build;
	}
}
