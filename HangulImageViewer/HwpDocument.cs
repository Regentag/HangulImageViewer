using AxHWPCONTROLLib;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace HangulImgViewer
{
    class HwpDocument : IDisposable
    {
        public enum HwpState
        {
            NotOpen,
            HmlReady,
            HmlAnalyzed,
            Disposed,
            ErrOpenFail,
            ErrHmlCreateFail,
            ErrHmlLoadFail,
        }

        private string hwpPath;
        private string hmlPath;
        private XmlDocument hmlDoc;

        public HwpDocument(string hwpPath)
        {
            this.hwpPath = hwpPath;
            hmlPath = null;
            hmlDoc = new XmlDocument();
            State = HwpState.NotOpen;
        }

        public HwpState State { get; set; }
        public string HwpFilePath { get { return hwpPath; } }

        public bool Open(AxHwpCtrl hwp)
        {
            if( hwp.Open(hwpPath, "HWP", "") )
            {
                var tmpName = Path.GetTempFileName();
                if( hwp.SaveAs(tmpName, "HWPML2X", "") )
                {
                    hmlPath = tmpName;
                    hwp.Clear("1"); // hwpDiscard: Discard document changes.

                    try
                    {
                        State = HwpState.HmlReady;
                        hmlDoc.Load(hmlPath);
                        return true;
                    }
                    catch
                    {
                        State = HwpState.ErrHmlLoadFail;
                        return false;
                    }
                }
                else
                {
                    State = HwpState.ErrHmlCreateFail;
                    return false;
                }
            }
            else
            {
                State = HwpState.ErrOpenFail;
                return false;
            }
        }

        /// <summary>
        /// 문서에 포함된 이미지의 개수를 가져옵니다.
        /// </summary>
        /// <returns>Numbers of Image</returns>
        public int GetImageCount()
        {
            if(State == HwpState.HmlReady && hmlDoc != null)
            {
                return hmlDoc.GetElementsByTagName("IMAGE").Count;
            }
            else
            {
                return -1;
            }
        }

        public string[] GetImageBinaryDataID()
        {
            return hmlDoc.GetElementsByTagName("IMAGE")
                .Cast<XmlNode>().Select(e => e.Attributes["BinItem"].Value)
                .ToArray();
        }

        public string GetImageBase64(string binItemId)
        {
            if(binItemId == null)
            {
                return "";
            }

            var find = hmlDoc.GetElementsByTagName("BINDATA")
                .Cast<XmlNode>().Where(e => binItemId.Equals(e.Attributes["Id"].Value))
                .FirstOrDefault();

            if(find != null)
            {
                return find.InnerText;
            }
            else
            {
                return "";
            }
        }

        public void Dispose()
        {
            if(File.Exists(hmlPath))
            {
                File.Delete(hmlPath);
            }
        }
    }
}
