// SoNG Browser Optimizer icon generator (Windows .NET Framework)
// Multi-resolution transparent icon, PNG-compressed (Windows Vista+).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
internal static class IconMaker
{
    static GraphicsPath Rounded(float x,float y,float w,float h,float r)
    {
        GraphicsPath p=new GraphicsPath(); float d=r*2;
        p.AddArc(x,y,d,d,180,90); p.AddArc(x+w-d,y,d,d,270,90);
        p.AddArc(x+w-d,y+h-d,d,d,0,90); p.AddArc(x,y+h-d,d,d,90,90);
        p.CloseFigure(); return p;
    }
    static void FillRound(Graphics g,Brush b,float x,float y,float w,float h,float r)
    { using(GraphicsPath p=Rounded(x,y,w,h,r)) g.FillPath(b,p); }
    static void StrokeRound(Graphics g,Pen pen,float x,float y,float w,float h,float r)
    { using(GraphicsPath p=Rounded(x,y,w,h,r)) g.DrawPath(pen,p); }
    static void Sparkle(Graphics g,Brush b,float x,float y,float r)
    {
        PointF[] pts={new PointF(x,y-r),new PointF(x+r*.25f,y-r*.25f),
            new PointF(x+r,y),new PointF(x+r*.25f,y+r*.25f),new PointF(x,y+r),
            new PointF(x-r*.25f,y+r*.25f),new PointF(x-r,y),new PointF(x-r*.25f,y-r*.25f)};
        g.FillPolygon(b,pts);
    }
    static Bitmap Draw(int size)
    {
        Bitmap bmp=new Bitmap(size,size,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent); g.SmoothingMode=SmoothingMode.AntiAlias;
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            g.CompositingQuality=CompositingQuality.HighQuality;
            g.ScaleTransform(size/256f,size/256f);
            using(Brush bg=new LinearGradientBrush(new Rectangle(8,8,240,240),
                Color.FromArgb(17,35,64),Color.FromArgb(21,91,143),LinearGradientMode.ForwardDiagonal))
                FillRound(g,bg,8,8,240,240,52);
            using(Pen edge=new Pen(Color.FromArgb(86,209,250),5))
                StrokeRound(g,edge,11,11,234,234,49);
            using(Brush shade=new SolidBrush(Color.FromArgb(10,30,55)))
                FillRound(g,shade,39,53,178,139,22);
            using(Brush screen=new SolidBrush(Color.FromArgb(25,58,102)))
                FillRound(g,screen,47,57,162,125,17);
            using(Pen frame=new Pen(Color.FromArgb(105,216,255),7))
                StrokeRound(g,frame,45,56,166,127,19);
            using(Brush stand=new SolidBrush(Color.FromArgb(202,236,254)))
            {
                g.FillRectangle(stand,115,184,26,14);
                FillRound(g,stand,89,198,79,13,5);
            }
            using(Brush chip=new SolidBrush(Color.FromArgb(43,145,176)))
                FillRound(g,chip,101,97,48,44,6);
            using(Pen pins=new Pen(Color.FromArgb(109,221,223),4))
            {
                for(int i=0;i<3;i++)
                {
                    float z=107+16*i;
                    g.DrawLine(pins,z,91,z,98); g.DrawLine(pins,z,140,z,148);
                    g.DrawLine(pins,94,103+i*14,102,103+i*14);
                    g.DrawLine(pins,149,103+i*14,156,103+i*14);
                }
            }
            g.TranslateTransform(114,133); g.RotateTransform(-41);
            using(Brush b=new SolidBrush(Color.FromArgb(248,254,255)))
            using(Brush accent=new SolidBrush(Color.FromArgb(110,227,221)))
            {
                FillRound(g,b,-5,-77,10,111,5);
                FillRound(g,accent,-17,25,34,12,5);
                PointF[] bristles={new PointF(-19,37),new PointF(19,37),
                    new PointF(29,67),new PointF(12,67),new PointF(5,60),new PointF(0,69),
                    new PointF(-8,61),new PointF(-17,67),new PointF(-29,67)};
                g.FillPolygon(b,bristles);
            }
            g.ResetTransform(); g.ScaleTransform(size/256f,size/256f);
            using(Brush white=new SolidBrush(Color.FromArgb(244,255,255)))
            using(Brush teal=new SolidBrush(Color.FromArgb(102,243,219)))
            {
                Sparkle(g,white,194,66,20); Sparkle(g,teal,218,105,10);
                Sparkle(g,white,68,82,8);
            }
        }
        return bmp;
    }
    static void Main(string[] args)
    {
        string outPath=args.Length>0?args[0]:"SoNG_Browser_Optimizer.ico";
        int[] sizes={16,24,32,48,64,128,256};
        byte[][] encoded=new byte[sizes.Length][];
        for(int i=0;i<sizes.Length;i++)
        {
            using(Bitmap bmp=Draw(sizes[i]))
            using(MemoryStream mem=new MemoryStream())
            {
                bmp.Save(mem,ImageFormat.Png); encoded[i]=mem.ToArray();
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        using(BinaryWriter w=new BinaryWriter(File.Create(outPath)))
        {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
            int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++)
            {
                w.Write((byte)(sizes[i]==256?0:sizes[i]));
                w.Write((byte)(sizes[i]==256?0:sizes[i]));
                w.Write((byte)0); w.Write((byte)0);
                w.Write((ushort)1); w.Write((ushort)32);
                w.Write((uint)encoded[i].Length); w.Write((uint)offset);
                offset+=encoded[i].Length;
            }
            for(int i=0;i<sizes.Length;i++) w.Write(encoded[i]);
        }
        Console.WriteLine("ICON_OK "+outPath);
    }
}
