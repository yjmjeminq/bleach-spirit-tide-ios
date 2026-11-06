using UnityEngine;
namespace SpiritTide
{
    public sealed partial class TideSkillFxGraphic
    {
        private void Frost()
        {
            int slot=effect.slot;Color ice=effect.core;float b=Burst;
            if(ground)
            {
                Vector2 at=slot==2?source:target;
                if(slot==0){Glow(at,new Vector2(90,23),ice);return;}
                if(slot==2){for(int i=0;i<5;i++)Snow(Forward(i*35),13+4*b,i*18,C(ice,.35f));return;}
                Sigil(at,(slot==4?210:90)*( .6f+b*.4f),ice);
                if(slot==1||slot==4)for(int i=0;i<6;i++)Snow(at+Vector2.Scale(Dir(i*60),new Vector2(slot==4?170:70,slot==4?43:20)),12+7*b,age*30,C(ice,.65f));
                return;
            }
            if(slot==0){Slash(Forward(45),85+25*b,-110+Progress*100,ice);Slash(Forward(85),60+30*b,80-Progress*95,Color.white,3);}
            if(slot==1){Ring(target,new Vector2(92,65),C(ice,.35f));for(int i=0;i<6;i++)Crystal(target+new Vector2((i-2.5f)*27,-58),30+28*b,9,78+i*5,ice);Snow(target,45,age*20,C(ice,.6f));}
            if(slot==2){for(int i=0;i<7;i++)Crystal(Forward(i*28,-58),20+22*b,5,110,ice);Sparks(Forward(110),12,120,ice);}
            if(slot==3){for(int i=0;i<3;i++){float grow=Mathf.Clamp01((Progress-.2f-i*.1f)*4);Crystal(target+new Vector2((i-1)*64,-55),125*grow,21,90+(i-1)*12,ice);}Snow(target+Vector2.up*80,27,age*32,C(Color.white,.7f));}
            if(slot==4){for(int i=0;i<9;i++){float x=(i-4)*44;Crystal(target+new Vector2(x,-60),60+85*b*(.65f+.35f*Mathf.Cos(i)),14,90-x*.09f,ice);}Snow(target+new Vector2(0,60),83,age*12,C(Color.white,.65f));Arc(target,new Vector2(220,140),age*18,280,3,C(ice,.5f));Sparks(target,30,205,ice);}
            Glow(target,new Vector2(slot==4?210:95,90),ice,.11f);
        }
        private void Flowers()
        {
            int slot=effect.slot;Color gold=effect.core;Vector2 at=slot==2?target:source;float b=Burst;
            if(ground){Sigil(at,slot==4?195:82,gold,slot==3?4:6);if(slot==1||slot==4)Ring(at,new Vector2(100+45*b,30+12*b),C(effect.accent,.3f));return;}
            if(slot==0||slot==3)
            {
                int petals=slot==0?3:4;Vector2 center=Forward(55,10);float radius=82*(.7f+b*.3f);
                for(int i=0;i<petals;i++)
                {Vector2 a=center+Dir(90+i*360f/petals)*radius,c=center+Dir(90+(i+1)*360f/petals)*radius;Beam(a,c,3,gold);Triangle(center,a,c,C(gold,.015f),C(gold,.08f),C(gold,.04f));Flower(a,12,6,age*60,C(Color.white,.65f));}
                Ring(center,Vector2.one*radius*1.12f,C(gold,.24f));
            }
            if(slot==1)
            {
                for(int i=0;i<2;i++){Vector2 pos=source+new Vector2((i==0?-1:1)*(38+10*Mathf.Sin(age*5)),10+30*Mathf.Sin(age*3+i*Mathf.PI));Flower(pos,38,6,age*(i==0?50:-50),C(gold,.6f));}
                Glow(source,new Vector2(95,125),new Color(.55f,1,.75f),.13f);Sparks(source,12,90,new Color(.65f,1,.85f),true);
            }
            if(slot==2){Vector2 end=Vector2.Lerp(Forward(40),target+new Vector2(facing*25,0),Mathf.Clamp01(Progress*2));Beam(Forward(20),end,9,gold);Flower(end,35+24*b,6,age*80,C(gold,.7f));Sparks(end,14,85,effect.accent);}
            if(slot==4)
            {
                Vector2 center=Forward(85,15);Ring(center,new Vector2(190,125),C(gold,.45f),3);
                for(int i=0;i<6;i++){Vector2 p=center+Vector2.Scale(Dir(i*60+age*18),new Vector2(177,115));Flower(p,26+8*b,6,-age*35,C(gold,.7f));Vector2 next=center+Vector2.Scale(Dir((i+1)*60+age*18),new Vector2(177,115));Beam(p,next,1.5f,gold);}
                Flower(center,65+20*b,6,age*25,C(Color.white,.4f));Glow(center,new Vector2(205,150),gold,.12f);Sparks(center,24,170,gold,true);
            }
        }
        private void Thunder()
        {
            int slot=effect.slot;Color violet=effect.core;float b=Burst;
            if(ground)
            {
                Vector2 at=slot==0||slot==2?Forward(85):target;Ring(at,new Vector2((slot==4?200:85)*( .6f+b),23+12*b),C(violet,.4f));
                if(slot==1||slot==4)for(int i=0;i<(reduced?3:7);i++)Lightning(at,at+Vector2.Scale(Dir(i*360f/7),new Vector2(slot==4?195:105,33)),4,7,effect.accent);
                return;
            }
            if(slot==0){for(int i=0;i<2;i++){Vector2 at=Forward(55+i*50,(i==0?-1:1)*15);Slash(at,58+25*b,i*180+Progress*90,violet,8);Lightning(source,at,6,12,effect.accent);}}
            if(slot==1){Lightning(target+new Vector2(5,170),target-new Vector2(0,45),12,21,violet);for(int i=0;i<3;i++)Arc(target,new Vector2(60+i*19,40+i*12),i*110+age*80,210,2,C(effect.accent,.6f));}
            if(slot==2){for(int i=0;i<3;i++){Vector2 from=Forward(-30+i*50,(i-1)*25),to=target+new Vector2(facing*30,(1-i)*18);Lightning(from,to,11,10+i*5,i%2==0?violet:effect.accent);}Sparks(target,18,125,violet);}
            if(slot==3){for(int i=0;i<3;i++)Arc(Forward(-i*24),new Vector2(45,78),-70,160,3,C(violet,.6f-i*.13f));Lightning(Forward(-80,-30),Forward(35,55),9,11,effect.accent);Flower(Forward(-15,65),22,2,110,C(violet,.6f));}
            if(slot==4){for(int i=0;i<3;i++)Lightning(target+new Vector2((i-1)*70,185),target+new Vector2((i-1)*25,-55),14,20+i*5,i==1?Color.white:violet);for(int i=0;i<5;i++)Slash(target,78+i*19,i*72+age*130,violet,4);Glow(target,new Vector2(180,140),violet,.13f);Sparks(target,28,190,effect.accent);}
        }
        private void Hornet()
        {
            int slot=effect.slot;Color gold=effect.core;float b=Burst;
            if(ground){if(slot==4)Sigil(source,110,gold,4);else Ring(target,new Vector2(45+35*b,14+10*b),C(gold,.3f));return;}
            if(slot==0){Beam(Forward(20),Vector2.Lerp(Forward(20),target,Mathf.Clamp01(Progress*3)),3,gold);Flower(target,28,2,30,C(gold,.75f));Ring(target,Vector2.one*33,C(gold,.55f));}
            if(slot==1){for(int i=0;i<4;i++){Vector2 at=Forward(i*38,-10+i*8);Line(at,at+new Vector2(facing*58,0),3,C(gold,.55f-i*.08f));Arc(at,new Vector2(20,48),facing>0?-80:100,160,2,C(effect.accent,.45f));}Flower(target,44,2,40,C(gold,.55f));}
            if(slot==2){for(int i=0;i<5;i++){float lane=(i-2)*15;Beam(Forward(15,lane),target+new Vector2(facing*15,-lane),2.2f,gold);}Sparks(target,14,75,gold);}
            if(slot==3){Flower(target,63+20*b,2,25,C(gold,.65f));Flower(target,58+30*b,2,155,C(effect.accent,.65f));for(int i=0;i<8;i++)Beam(target+Dir(i*45)*(25+30*b),target+Dir(i*45)*(60+70*b),2,gold);Ring(target,Vector2.one*(40+100*b),C(gold,.4f));}
            if(slot==4)
            {
                Vector2 muzzle=Forward(55);for(int i=0;i<3;i++)Ring(muzzle+new Vector2(facing*i*15,0),new Vector2(12+i*5,36+i*12),C(gold,.6f),3);
                if(Progress>.3f){Beam(muzzle,Forward(430),20*(1-Mathf.Clamp01(Progress-1)*.7f),gold);Beam(muzzle+new Vector2(0,12),Forward(360,15),2,effect.accent);}
                Sparks(muzzle,22,125,gold);Glow(muzzle,new Vector2(75,90),gold,.17f);
            }
        }
        private void Ash()
        {
            int slot=effect.slot;Color silver=effect.core;float b=Burst;
            if(ground)
            {
                Vector2 at=slot==2?target:Forward(100);float radius=slot==4?205:110;
                for(int i=0;i<(reduced?2:4);i++)Arc(at,new Vector2(radius*(.5f+i*.17f),radius*(.12f+i*.04f)),age*(slot==2?-100:70)+i*85,235,1.5f,C(silver,.3f));
                Glow(at,new Vector2(radius,40),silver,.08f);return;
            }
            if(slot==0)
            {for(int i=0;i<(reduced?7:18);i++){float angle=-40+i*80f/(reduced?6:17),dist=45+b*110+i%3*10;Vector2 at=source+new Vector2(facing*Mathf.Cos(angle*Mathf.Deg2Rad)*dist,Mathf.Sin(angle*Mathf.Deg2Rad)*dist);Crystal(at,18,4,angle+(facing>0?0:180),silver);}}
            if(slot==1)
            {for(int i=0;i<5;i++){Vector2 at=Forward(110+(i-2)*23,Mathf.Sin(age*3+i)*18);Glow(at,new Vector2(75,52),silver,.06f);Arc(at,new Vector2(60,35),i*73+age*45,250,1,C(silver,.4f));}Sparks(Forward(110),18,100,silver);}
            if(slot==2)
            {for(int i=0;i<9;i++){float angle=i*40+age*170;Vector2 at=target+Vector2.Scale(Dir(angle),new Vector2(125*(1-b*.55f),70*(1-b*.55f)));Crystal(at,22,4,angle+100,silver);Arc(target,new Vector2(85,55),angle,85,2,C(effect.accent,.4f));}Sparks(target,15,150,silver,true);}
            if(slot==3)
            {for(int i=0;i<3;i++){Vector2 at=target+new Vector2((i-1)*85,(i%2)*23);float spread=30+Mathf.Clamp01(Progress-.1f-i*.13f)*85;Ring(at,new Vector2(spread,spread*.7f),C(silver,.6f));Sparks(at,9,spread,silver);}}
            if(slot==4)
            {for(int i=0;i<(reduced?12:27);i++){float angle=i*37+age*160,radius=55+i%5*25;Vector2 at=target+Vector2.Scale(Dir(angle),new Vector2(radius,radius*.6f));Crystal(at,22,5,angle+90,i%4==0?effect.accent:silver);}for(int i=0;i<6;i++){Vector2 band=target+new Vector2(0,-55+i*29);Vector2 radius=new Vector2(145-i*15,28);Arc(band,radius,age*180-i*60,270,10,C(silver,.12f));Arc(band,radius,age*180-i*60,270,1.5f,C(silver,.7f));}Sparks(target,20,200,silver,true);}
        }
        private void Crimson()
        {
            int slot=effect.slot;Color red=effect.core,mint=effect.accent;float b=Burst;
            if(ground)
            {Sigil(source,slot==4?205:slot==0?105:70,slot==0?mint:red,slot==2?4:8);if(slot==3){Arc(source,new Vector2(92,27),age*25,180,3,C(red,.55f));Arc(source,new Vector2(92,27),age*25+180,180,3,C(mint,.55f));}return;}
            if(slot==0){Flower(source,50,8,age*20,C(mint,.55f));for(int i=0;i<3;i++)Ring(source+new Vector2(0,-45+i*35),new Vector2(80-i*12,22),C(mint,.25f));Sparks(source,15,90,mint,true);Glow(source,new Vector2(100,120),mint,.11f);}
            if(slot==1){Slash(Forward(55),125+35*b,-80+Progress*45,red,9);Slash(Forward(60),105,100-Progress*45,Color.white,2);Sparks(target,14,95,red);}
            if(slot==2){Beam(Forward(0,-45),Forward(95,65),5,red);Beam(Forward(0,65),Forward(95,-45),3,Color.white);Arc(source,new Vector2(95,110),-70,140,2,C(mint,.45f));}
            if(slot==3){Arc(source,new Vector2(75,95),age*45,180,4,C(red,.65f));Arc(source,new Vector2(75,95),age*45+180,180,4,C(mint,.65f));Flower(source,35+20*b,4,age*30,C(Color.white,.4f));}
            if(slot==4){for(int i=0;i<5;i++)Slash(target,new Vector2(105+i*20,0).x,i*72+Progress*85,red,6);Ring(target,new Vector2(190,125),C(red,.45f));Flower(source,50+35*b,8,-age*30,C(mint,Progress>1?.65f:.2f));Glow(target,new Vector2(200,130),red,.09f);Sparks(target,26,190,Progress>1?mint:red);}
        }
    }
}
