using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace hwpx
{
    public class ContentItem
    {
        public string ID { get; set; }
        public string MediaType { get; set; }
        public string HRef { get; set; }
        public string IsEmbeded { get; set; }
    }

    /// <summary>
    /// .hwpx 파일
    /// </summary>
    public class HwpxDocument
    {
        private string filename;

        private ZipArchive file;
        private XmlDocument content;

        public HwpxDocument(string filename)
        {
            this.filename = filename;
        }

        public string FileName { get { return filename; } }

        public bool Open()
        {
            file = new ZipArchive(File.Open(filename, FileMode.Open, FileAccess.ReadWrite));
            var contentEntry = file.GetEntry("Contents/content.hpf");
            if( contentEntry != null )
            {
                content = new XmlDocument();
                using (var stream = contentEntry.Open())
                {
                    content.Load(stream);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public ContentItem[] GetContentItemList()
        {
            return content.GetElementsByTagName("opf:item")
                .Cast<XmlElement>()
                .Select(e =>
                {
                    var item = new ContentItem();
                    item.ID = e.GetAttribute("id");
                    item.MediaType = e.GetAttribute("media-type");
                    item.HRef = e.GetAttribute("href");
                    item.IsEmbeded = e.GetAttribute("isEmbeded");
                    return item;
                })
                .ToArray();
        }

        /// <summary>
        /// Update opf:item tag's attributes in Contents/content.hpf entry.
        /// If item not exist and createIfNotExist is TRUE,
        /// add a new opf: item element to the end of the list.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="createIfNotExist"></param>
        public void UpdateContentItem(ContentItem item, bool createIfNotExist=false)
        {
            var elem = content.GetElementsByTagName("opf:item")
                .Cast<XmlElement>()
                .Where(e =>
                {
                    return item.ID.Equals(e.GetAttribute("id"));
                })
                .FirstOrDefault();

            if(elem != null)
            {
                elem.SetAttribute("media-type", item.MediaType);
                elem.SetAttribute("href", item.HRef);

                // isEmbeded attribute는 값이 1일 경우에만 생성.
                // 그렇지 않을 경우 attribute를 삭제한다.
                if("1".Equals(item.IsEmbeded))
                {
                    elem.SetAttribute("isEmbeded", "1");
                }
                else
                {
                    if(elem.HasAttribute("isEmbeded"))
                    {
                        elem.RemoveAttribute("isEmbeded");
                    }
                }
            }
            else
            {
                if(createIfNotExist)
                {
                    var mani = content.GetElementsByTagName("opf:manifest")
                        .Cast<XmlElement>()
                        .FirstOrDefault();
                    if(mani != null )
                    {
                        var newElem = mani.OwnerDocument.CreateElement("opf:item");
                        newElem.SetAttribute("media-type", item.MediaType);
                        newElem.SetAttribute("href", item.HRef);

                        // isEmbeded attribute는 값이 1일 경우에만 생성.
                        if ("1".Equals(item.IsEmbeded))
                        {
                            newElem.SetAttribute("isEmbeded", "1");
                        }
                        mani.AppendChild(newElem);
                    }
                    else
                    {
                        throw new Exception("Invalid content.hpf; <opf:manifest> not found.");
                    }
                }
            }
        }

        /// <summary>
        /// Commit changes in Contents/content.hpf entry.
        /// </summary>
        public void CommitContentChanges()
        {
            
        }

        public Stream GetEntryStream(string entryName)
        {
            return file.GetEntry(entryName).Open();
        }

        public long GetEntrySize(string entryName)
        {
            return file.GetEntry(entryName).Length;
        }

        public void DeleteEntry(string entryName)
        {
            file.GetEntry(entryName).Delete();
        }

        public Stream CreateEntry(string entryName)
        {
            return file.CreateEntry(entryName).Open();
        }

        public void Close()
        {
            file.Dispose();
        }
    }
}
