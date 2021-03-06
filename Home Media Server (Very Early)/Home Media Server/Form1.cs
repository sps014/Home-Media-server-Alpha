﻿using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sonic.Server;
namespace Home_Media_Server
{
    public partial class Form1 : Form
    {

       SonicHttpServer server = new SonicHttpServer();
       //HttpServer server = new HttpServer();

        public Form1()
        {
            InitializeComponent();
        }

        private  void button1_Click(object sender, EventArgs e)
        {

            if (!server.IsRunning)
            {
                server.Start();
                button1.Text = "Stop";
            }
            else
            {
                button1.Text = "Start";
                server.Stop();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //server.OnConnection += Server_OnConnection;
            server.ClientConnected += Server_ClientConnected;
            server.Start();
        }

        private async void Server_ClientConnected(object sender, ConnectionEventArgs e)
        {
            
            string reqURL = e.Request.URL;
            if(reqURL==null)
            {
                e.Response.OutputStream.Close();
                return;
            }
            Invoke((MethodInvoker)delegate ()
            {
                textBox2.AppendText(reqURL);
            });

            if (reqURL == "/")
            {
                e.Response.WriteHeader();
                string s = "<head><meta name='viewport' content='width=device-width,inital-scale=1'/><title>Stage Streaming </title></head>";
                e.Response.Write(" <html>"+s+"<body> <video style='left:0;top:0;' width='100%' height='100%' controls autoplay><source src='/video' type ='video/mp4'/></video></body></html>");
                e.Response.End();
            }
            else if (reqURL == "/video")
            {
                string filePath = textBox1.Text;

                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    int startByte = -1;
                    int endByte = -1;
                    int byteRange = -1;
                    int maxChunk = 1024 * 1024;

                    string Range = e.Request.Range;

                    long total = new FileInfo(filePath).Length;

                    if (Range != null)
                    {
                        string rangeHeader = Range.Replace("bytes=", "");
                        string[] range = rangeHeader.Split('-');
                        startByte = int.Parse(range[0]);
                        if (range[1].Trim().Length > 0)
                            int.TryParse(range[1], out endByte);
                        if (endByte == -1)
                        {
                            endByte = (int)fs.Length;
                        }
                    }
                    else
                    {
                        startByte = 0;
                        endByte = (int)fs.Length;
                    }

                    //Chunks splitting here on....
                    if (startByte + maxChunk < total)
                        endByte = startByte + maxChunk;
                    else
                        endByte = (int)total;

                    byteRange = endByte - startByte;

                    byte[] buffer = new byte[byteRange];
                    fs.Position = startByte;
                    fs.Read(buffer, 0, byteRange);
                    fs.Flush();
                    fs.Close();

                    string res = "HTTP/2.0 206 Partial Content\r\nContent-Type:video/mp4\r\n";
                    string cnt = "Content-Length:" + byteRange + "\r\n";
                    string acr = "Accept-Ranges:bytes\r\n";
                    string crg = "Content-Range:" + string.Format("bytes {0}-{1}/{2}\r\n", startByte, startByte + byteRange-1, total);
                    string cst = "Connection:keep-alive\r\n\r\n";
                    string msg = res+cnt + acr + crg + cst;
                    byte[] msgData = Tobyte(msg);
                    e.Response.OutputStream.Write(msgData,0,msgData.Length);

                    int totalCount = (int)total;
                    try
                    {
                        await e.Response.OutputStream.WriteAsync(buffer,0,buffer.Length);
                        e.Response.OutputStream.Flush();
                        e.Response.OutputStream.Close();
                        
                    }
                    catch(Exception en)
                    {
                        MessageBox.Show(this,en.Message,"Warning",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    }



                }
            }
            else
            {
                e.Response.WriteHeader(StatusCode.NOT_FOUND, MIMEType.none);
                e.Response.End();
            }
            Invoke((MethodInvoker)delegate ()
            {
                switch(reqURL)
                {
                    case "/":
                        textBox2.AppendText( "\ttext/html \t\t->200 OK" + "\r\n\r\n");
                        break;
                    case "/video":
                        textBox2.AppendText("\tvideo/mp4\t->206 Partial Content" + "\r\n\r\n");
                        break;
                    default:
                        textBox2.AppendText("\ttext\t->404 Not Found" + "\r\n\r\n");
                        break;
                }
            });
        }

        public byte[] Tobyte(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        private async void Server_OnConnection(object sender, HttpListenerContext e)
        {
   
            HttpListenerContext context = e;
            HttpListenerRequest request = e.Request;
            HttpListenerResponse response = e.Response;
            if (e.Request.Url.AbsolutePath == "/")
            {
                e.Response.StatusCode = (int)StatusCode.OK;
                response.StatusDescription = "OK";
                response.Headers.Add("Content-Type", "text/html");
                var buffer = Tobyte(" <html><head><title>winForm </title></head><body> <video width='100%' height='100%' controls autoplay><source src='/video' type ='video/mp4'/></video></body></html>");
                e.Response.OutputStream.Write(buffer,0,buffer.Length);
                e.Response.OutputStream.Flush();
                e.Response.Close();
            }
            else if (e.Request.Url.AbsolutePath == "/video")
            {
                string filePath = textBox1.Text;

                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    int startByte = -1;
                    int endByte = -1;
                    int byteRange = -1;
                    int maxChunk = 1024 * 1024;

                    long total = new FileInfo(filePath).Length;

                    if (request.Headers.GetValues("Range") != null)
                    {
                        string rangeHeader = request.Headers.GetValues("Range")[0].Replace("bytes=", "");
                        string[] range = rangeHeader.Split('-');
                        startByte = int.Parse(range[0]);
                        if (range[1].Trim().Length > 0)
                            int.TryParse(range[1], out endByte);
                        if (endByte == -1)
                        {
                            endByte = (int)fs.Length;
                        }
                    }
                    else
                    {
                        startByte = 0;
                        endByte = (int)fs.Length;
                    }

                    //Chunks splitting here on....
                    if (startByte + maxChunk < total)
                        endByte = startByte + maxChunk;
                    else
                        endByte = (int)total;

                    byteRange = endByte - startByte;

                    byte[] buffer = new byte[byteRange];
                    fs.Position = startByte;
                    fs.Read(buffer, 0, byteRange);
                    fs.Flush();
                    fs.Close();

                    response.StatusCode = (int)HttpStatusCode.PartialContent;
                    response.StatusDescription = "Partial Content";
                    response.Headers.Add("Content-Type", "video/mp4");
                    response.Headers.Add("Accept-Ranges", "bytes");
                    response.ContentLength64 = byteRange;
                    response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", startByte, startByte + byteRange-1, total));
                    response.Headers.Add("Connection", "keep-alive");
                    try
                    {
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.OutputStream.Flush();
                    }
                    catch
                    { }



                }
            }
        }
    }
}
