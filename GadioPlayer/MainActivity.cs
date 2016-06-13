using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;

using HtmlAgilityPack;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Net;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GadioPlayer
{

    public class HomeScreenAdapter : BaseAdapter<GadioItem>
    {
        public List<GadioItem> items;
        Activity context;
        public HomeScreenAdapter(Activity context, List<GadioItem> items)
            : base()
        {
            this.context = context;
            this.items = items;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override GadioItem this[int position]
        {
            get { return items[position]; }
        }
        public override int Count
        {
            get { return items.Count; }
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];
            View view = convertView;
            if (view == null) // no view to re-use, create new
                view = context.LayoutInflater.Inflate(Resource.Layout.CustomView, null);
            view.FindViewById<TextView>(Resource.Id.Title).Text = item.Title + ":\n" + item.Description;

            view.FindViewById<ImageView>(Resource.Id.Image).SetImageBitmap(item.FrontCover);

            return view;
        }
    }

    [DataContract]
    public class GadioItem
    {
        [DataMember(Order = 0)]
        public string Title;
        [DataMember(Order = 1)]
        public string Description;
        [DataMember(Order = 2)]
        public string Channel;
        [DataMember(Order = 3)]
        public DateTime PostDate;
        [DataMember(Order = 4)]
        public Uri Link;
        [DataMember(Order = 5)]
        public Uri FrontCoverImg;
        [DataMember(Order = 6)]
        public Uri ChannelLink;

        public Bitmap FrontCover;
    }
    public static class UrlHelper
    {
        static string PageHtmlFormat = @"http://www.g-cores.com/categories/9/originals?page={0}";
        public static string getListUrl(int page)
        {
            return string.Format(PageHtmlFormat, page);
        }
        public static List<GadioItem> getItemsByUrl(string url)
        {
            List<GadioItem> GadioItemList = new List<GadioItem>();
            HtmlDocument GadioDoc = new HtmlDocument();
            WebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            using (var response = request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    GadioDoc.Load(sr);
                }
            }
            //DO content handling
            Parallel.ForEach(GadioDoc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("showcase-audio")), node =>
            {
                //Console.WriteLine("Gadio Hit!");
                GadioItem thisItem = new GadioItem();

                string ImgUrl = node.Descendants("img").ToList()[0].Attributes["src"].Value;
                thisItem.FrontCoverImg = new Uri(ImgUrl);



                var titleNode = node.Descendants("h4").ToList()[0];//#

                string title = titleNode.ChildNodes[1].InnerHtml.Trim();
                thisItem.Title = title;
                string link = titleNode.ChildNodes[1].Attributes[0].Value;
                thisItem.Link = new Uri(link);

                var childs = node.ChildNodes.Where(d => d.NodeType != HtmlNodeType.Text).ToList();//#
                string channel = childs[0].ChildNodes[1].ChildNodes[1].InnerHtml.Trim().Replace("\n", "").Replace(" ", "");
                thisItem.Channel = channel;
                string channel_link = childs[0].ChildNodes[1].ChildNodes[1].Attributes[0].Value;
                thisItem.ChannelLink = new Uri(channel_link);
                DateTime post_date = DateTime.Parse(childs[0].ChildNodes[2].InnerText.Trim());
                thisItem.PostDate = post_date;
                var description = childs[3].InnerText;
                thisItem.Description = description;

                WebRequest requestCover = WebRequest.CreateHttp(thisItem.FrontCoverImg.AbsoluteUri);
                requestCover.Method = "GET";

                using (var responseCover = requestCover.GetResponse())
                {
                    using (var stream = responseCover.GetResponseStream())
                    {
                        thisItem.FrontCover = BitmapFactory.DecodeStream(stream);
                    }
                }

                GadioItemList.Add(thisItem);
            });
            GadioItemList.Sort((a, b) => { return (int)((b.PostDate - a.PostDate).TotalSeconds); });
            return GadioItemList;
        }
        public static List<GadioItem> getItemsByPage(int page)
        {
            return getItemsByUrl(getListUrl(page));
        }
    }



    [Activity(Label = "GadioPlayer", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int current_page = 1;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            List<GadioItem> frontPageItems = UrlHelper.getItemsByPage(current_page++);
            var listView = FindViewById<ListView>(Resource.Id.listView1);
            listView.Adapter = new HomeScreenAdapter(this, frontPageItems);


            listView.ScrollStateChanged += (s, e) =>
            {
                if (e.ScrollState == ScrollState.Idle)
                {
                    if (listView.LastVisiblePosition == listView.Count - 1)
                    {
                        var lastpos = listView.LastVisiblePosition;
                        var toLoad = UrlHelper.getItemsByPage(current_page++);
                        foreach (var t in toLoad)
                            frontPageItems.Add(t);
                        Console.WriteLine("DEBUGGING:" + listView.Adapter.Count);
                        ((HomeScreenAdapter)listView.Adapter).NotifyDataSetChanged();
                    }
                }
            };

        }
    }
}

