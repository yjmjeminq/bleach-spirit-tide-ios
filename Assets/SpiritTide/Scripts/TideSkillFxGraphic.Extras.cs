using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TideSkillFxGraphic
    {
        private void ExtraEffect()
        {
            int skill=effect.slot,hero=effect.hero;
            Color hue=effect.core,accent=effect.accent;
            float burst=Burst;
            if(ground)
            {
                bool atTarget=hero==4&&skill!=7||hero==3&&skill==5||hero==2&&skill==6;
                Vector2 at=atTarget?target:source;
                float radius=Mathf.Lerp(54,skill==8?146:105,burst);
                Sigil(at,radius,hero==5&&skill==5?accent:hue,skill==7?4:6);
                return;
            }

            switch(hero)
            {
                case 0:ExtraFrost(skill,hue,burst);break;
                case 1:ExtraFlowers(skill,hue,burst);break;
                case 2:ExtraThunder(skill,hue,burst);break;
                case 3:ExtraHornet(skill,hue,burst);break;
                case 4:ExtraAsh(skill,hue,burst);break;
                case 5:ExtraCrimson(skill,hue,accent,burst);break;
            }
        }

        private void ExtraFrost(int skill,Color hue,float burst)
        {
            switch(skill)
            {
                case 5:
                    float spread=50+100*burst;
                    Ring(source,new Vector2(spread,spread*.44f),C(hue,.65f),3);
                    for(int i=0;i<6;i++)
                    {
                        Vector2 at=source+Vector2.Scale(Dir(i*60+age*24),new Vector2(spread,spread*.44f));
                        Snow(at,18+burst*9,age*33,C(hue,.75f));
                    }
                    break;
                case 6:
                    for(int i=0;i<5;i++)Crystal(Forward(i*31,-54),22+14*burst,6,90+facing*18,hue);
                    Slash(Forward(85+48*burst),105,-55+Progress*70,hue,8);
                    break;
                case 7:
                    for(int i=-2;i<=2;i++)Crystal(source+new Vector2(i*29,-42),88+28*burst-Mathf.Abs(i)*16,12,90+i*8,hue);
                    Ring(source,new Vector2(85,68),C(hue,.44f));
                    break;
                case 8:
                    Slash(Forward(113),120,18-Progress*100,hue,7);
                    for(int i=0;i<(reduced?6:13);i++)
                        Crystal(Forward(72+i*10+(i%3)*21*burst,Mathf.Sin(i*4)*(28+burst*30)),26,7,i*137,hue);
                    Sparks(Forward(120),19,138,hue);
                    break;
            }
        }

        private void ExtraFlowers(int skill,Color hue,float burst)
        {
            switch(skill)
            {
                case 5:
                    for(int ring=0;ring<2;ring++)
                    {
                        float radius=54+ring*27+burst*13;
                        for(int i=0;i<6;i++)
                            Beam(source+Dir(i*60+30)*radius,source+Dir((i+1)*60+30)*radius,2+ring,hue);
                    }
                    break;
                case 6:
                    for(int i=-2;i<=2;i++)
                    {
                        Vector2 at=Forward(52+(180-Mathf.Abs(i)*15)*burst,i*(14+burst*18));
                        Beam(Forward(22,i*10),at,2.4f,hue);
                        Flower(at,15,4,age*67+i*12,C(hue,.7f));
                    }
                    break;
                case 7:
                    Arc(source,new Vector2(74,66),age*125,135,4,C(hue,.65f));
                    Arc(source,new Vector2(74,66),180+age*125,135,4,C(hue,.65f));
                    Flower(source,34,6,-age*35,C(hue,.7f));
                    Sparks(source,13,96,hue,true);
                    break;
                case 8:
                    Color mint=new Color(.54f,1,.78f);
                    for(int i=0;i<4;i++)Flower(source+new Vector2((i-1.5f)*37,i*12-37+burst*45),24,6,age*35+i*15,C(mint,.7f));
                    Glow(source,new Vector2(113,125),mint,.13f);
                    break;
            }
        }

        private void ExtraThunder(int skill,Color hue,float burst)
        {
            switch(skill)
            {
                case 5:
                    Vector2 end=Forward(42+145*burst,-7);
                    Lightning(source+Vector2.up*36,end,8,17,hue);
                    Lightning(Forward(35,-45),end,5,9,effect.accent);
                    Slash(end,62,150-Progress*86,hue,9);
                    break;
                case 6:
                    for(int i=0;i<4;i++)
                    {
                        float beat=Mathf.Clamp01((Progress-.08f-i*.16f)*5);
                        if(beat>0)Lightning(target+Dir(35+i*90)*82,target+Dir(35+i*90)*20*(1-beat),5,8,hue);
                    }
                    Ring(target,new Vector2(30+burst*18,24+burst*18),C(effect.accent,.42f));
                    break;
                case 7:
                    Lightning(source,target+new Vector2(facing*58,17),9,14,hue);
                    Arc(target+new Vector2(facing*50,0),new Vector2(52,75),-20+age*105,240,3,C(hue,.69f));
                    break;
                case 8:
                    Lightning(source+new Vector2(0,174),source-new Vector2(0,48),12,24,hue);
                    for(int i=0;i<4;i++)Lightning(source-new Vector2(0,43),source+new Vector2((i-1.5f)*92*burst,-53+i%2*35),5,10,effect.accent);
                    Ring(source-new Vector2(0,45),new Vector2(75+burst*96,26+burst*12),C(hue,.54f),3);
                    break;
            }
        }

        private void ExtraHornet(int skill,Color hue,float burst)
        {
            switch(skill)
            {
                case 5:
                    Beam(Forward(22),Vector2.Lerp(Forward(22),target,burst),4,hue);
                    for(int i=0;i<4;i++)
                        Line(target+Dir(45+i*90)*25,target+Dir(135+i*90)*25,2,C(hue,.64f));
                    break;
                case 6:
                    Beam(Forward(20,-2),Forward(72+235*burst,-2),8,hue);
                    Arc(Forward(85),new Vector2(22,69),-28+age*63,240,3,C(effect.accent,.55f));
                    Sparks(Forward(117),13,83,hue);
                    break;
                case 7:
                    for(int i=0;i<4;i++)
                    {
                        Vector2 trail=Forward(-i*32,-12+i*7);
                        Arc(trail,new Vector2(21,47),facing>0?100:-80,160,2,C(hue,.6f-i*.1f));
                    }
                    Beam(Forward(10),Forward(145*burst+25),3,hue);
                    break;
                case 8:
                    Slash(Forward(70),115,20-Progress*80,hue,4);
                    for(int i=0;i<4;i++)Beam(target+Dir(i*90+35)*92,target+Dir(i*90+35)*21,2.5f,hue);
                    Flower(target,35+burst*18,4,-age*32,C(hue,.57f));
                    break;
            }
        }

        private void ExtraAsh(int skill,Color hue,float burst)
        {
            switch(skill)
            {
                case 5:
                    for(int i=0;i<6;i++)
                    {
                        Vector2 at=target+new Vector2((i-2.5f)*23,78*(1-burst)-i%3*23);
                        Crystal(at,23,4,-95+i*5,hue);
                        Glow(at,new Vector2(46,40),hue,.11f);
                    }
                    break;
                case 6:
                    for(int i=0;i<5;i++)
                    {
                        float distance=120-i*14-burst*55;
                        Arc(target,new Vector2(distance,distance*.48f),i*73+age*230,100,3,C(hue,.5f));
                    }
                    Sparks(target,18,138,hue,true);
                    break;
                case 7:
                    for(int i=0;i<5;i++)
                        Arc(source+new Vector2(0,(i-2)*17),new Vector2(65+burst*24,33),i*37+age*150,180,4,C(hue,.48f));
                    break;
                case 8:
                    for(int i=0;i<3;i++)Ring(target,new Vector2(23+burst*91+i*14,19+burst*55+i*7),C(hue,.51f-i*.1f),2);
                    Sparks(target,24,132,hue);
                    break;
            }
        }

        private void ExtraCrimson(int skill,Color hue,Color mint,float burst)
        {
            switch(skill)
            {
                case 5:
                    Beam(Forward(-57),Forward(-25),2.5f,mint);
                    Beam(Forward(-25),Forward(-12,31),2.5f,mint);
                    Beam(Forward(-12,31),Forward(6,-34),2.5f,mint);
                    Beam(Forward(6,-34),Forward(22),2.5f,mint);
                    Beam(Forward(22),Forward(57),2.5f,mint);
                    Ring(source,new Vector2(65+burst*42,48+burst*14),C(mint,.39f));
                    break;
                case 6:
                    Slash(Forward(94),138,Progress*80-115,hue,10);
                    Beam(Forward(25,-30),Forward(210*burst+25,31),2.8f,hue);
                    break;
                case 7:
                    Arc(source,new Vector2(60,87),age*120,178,3,C(mint,.66f));
                    Arc(source,new Vector2(60,87),age*120+180,178,3,C(hue,.66f));
                    Beam(Forward(-37,44),Forward(37,-44),3,hue);
                    Beam(Forward(-37,-44),Forward(37,44),3,mint);
                    break;
                case 8:
                    Slash(Forward(67),116,-90+Progress*82,hue,8);
                    Slash(Forward(-67),116,90+Progress*82,hue,6);
                    Flower(source,39+burst*26,6,age*30,C(mint,.68f));
                    break;
            }
        }
    }
}
