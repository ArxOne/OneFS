﻿
namespace ArxOne.OneFilesystem.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicTest
    {
        private IEnumerable<Tuple<OnePath, NetworkCredential>> EnumerateCredentials()
        {
            const string credentialsTxt = "credentials.txt";
            if (!File.Exists(credentialsTxt))
                Assert.Inconclusive("File '{0}' not found", credentialsTxt);
            using (var streamReader = File.OpenText(credentialsTxt))
            {
                for (; ; )
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        yield break;

                    var uri = new Uri(line);
                    var l = HttpUtility.UrlDecode(uri.UserInfo.Replace("_at_", "@"));
                    var up = l.Split(new[] { ':' }, 2);
                    yield return Tuple.Create(new OnePath(uri), new NetworkCredential(up[0], up[1]));
                }
            }
        }

        private Tuple<OnePath, NetworkCredential> GetTestCredential(string protocol)
        {
            var t = EnumerateCredentials().FirstOrDefault(c => c.Item1.Protocol == protocol);
            if (t == null)
                Assert.Inconclusive("Found no configuration for protocol '{0}'", protocol);
            return t;
        }

        [TestMethod]
        [DeploymentItem("credentials.txt")]
        [TestCategory("File")]
        [TestCategory("Ftp")]
        public void FtpEnumerateFilesTest()
        {
            EnumerateFilesTest("ftp");
        }

        [TestMethod]
        [DeploymentItem("credentials.txt")]
        [TestCategory("File")]
        [TestCategory("Ftpes")]
        public void FtpesEnumerateFilesTest()
        {
            EnumerateFilesTest("ftpes");
        }

        private void EnumerateFilesTest(string protocol)
        {
            var t = GetTestCredential(protocol);
            using (var fs = new OneFilesystem(t.Item2))
            {
                var l = fs.EnumerateEntries(t.Item1).ToArray();
            }
        }

        [TestMethod]
        [DeploymentItem("credentials.txt")]
        [TestCategory("File")]
        [TestCategory("Ftp")]
        public void FtpCreateDirectoryTest()
        {
            CreateDirectoryTest("ftp");
        }

        private void CreateDirectoryTest(string protocol)
        {
            var t = GetTestCredential(protocol);
            using (var fs = new OneFilesystem(t.Item2))
            {
                var folderPath = t.Item1 + ("folder-" + Guid.NewGuid().ToString());
                try
                {
                    Assert.IsTrue(fs.CreateDirectory(folderPath));
                }
                finally
                {
                    fs.Delete(folderPath);
                }
            }
        }

        [TestMethod]
        [DeploymentItem("credentials.txt")]
        [TestCategory("File")]
        [TestCategory("Ftp")]
        public void FtpCreateFileTest()
        {
            CreateFileTest("ftp");
        }

        private void CreateFileTest(string protocol)
        {
            var t = GetTestCredential(protocol);
            using (var fs = new OneFilesystem(t.Item2))
            {
                var filePath = t.Item1 + ("file-" + Guid.NewGuid().ToString());
                try
                {
                    using (var w = fs.CreateFile(filePath))
                        w.WriteByte(1);
                    using (var r = fs.OpenRead(filePath))
                    {
                        Assert.AreEqual(1, r.ReadByte());
                        Assert.AreEqual(-1, r.ReadByte());
                    }
                }
                finally
                {
                    fs.Delete(filePath);
                }
            }
        }
    }
}