using AxHWPCONTROLLib;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using hwpx;

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
        private string xPath;
        private HwpxDocument xDoc;

        public HwpDocument(string hwpPath)
        {
            this.hwpPath = hwpPath;
            xPath = null;
            xDoc = null;
            State = HwpState.NotOpen;
        }

        public HwpState State { get; set; }
        public string HwpFilePath { get { return hwpPath; } }

        public bool Open(AxHwpCtrl hwp)
        {
            if( hwp.Open(hwpPath, "HWP", "") )
            {
                var tmpName = Path.GetTempFileName();
                if( hwp.SaveAs(tmpName, "HWPX", "") )
                {
                    hwp.Clear("1"); // hwpDiscard: Discard document changes.

                    xPath = tmpName;
                    xDoc = new HwpxDocument(xPath);

                    if(xDoc.Open())
                    {
                        State = HwpState.HmlReady;
                        return true;
                    }
                    else
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
            if(State == HwpState.HmlReady && xDoc != null)
            {
                return xDoc.GetContentItemList()
                    .Where(item => item.MediaType.StartsWith("image/"))
                    .Count();
            }
            else
            {
                return -1;
            }
        }

        public string[] GetImageBinaryDataID()
        {
            return xDoc.GetContentItemList()
                .Where(item => item.MediaType.StartsWith("image/"))
                .Select(item => item.ID)
                .ToArray();
        }

        public Stream GetImageStream(string itemId)
        {
            var entryName = xDoc.GetContentItemList()
                .Where(item => item.ID.Equals(itemId))
                .FirstOrDefault()
                .HRef;
            return xDoc.GetEntryStream(entryName);
        }

        public void Dispose()
        {
            if(xDoc != null )
            {
                xDoc.Close();
            }

            if(File.Exists(xPath))
            {
                File.Delete(xPath);
            }
        }
    }
}
