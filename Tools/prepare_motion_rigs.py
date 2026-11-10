"""Build source-preserving UV meshes and two-bone skin bindings; never edit PNG pixels."""
from pathlib import Path
import colorsys
import hashlib
import json
import math
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'Assets/SpiritTide/Resources/SpiritTide/Data'
HEROES=['rukia','orihime','yoruichi','soifon','rangiku','unohana']
# All landmarks are authored in the original 1024x1536 PNG coordinate system.
# near/far mean painter's foreground/background, not anatomical left/right.
LANDMARKS=[
 dict(pelvis=(405,655),chest=(400,375),neck=(392,260),head=(395,155),shoulders=[(280,300),(480,305)],elbows=[(185,500),(575,510)],wrists=[(82,700),(700,710)],hips=[(331,687),(475,687)],knees=[(286,1000),(497,1000)],ankles=[(211,1425),(539,1420)],soles=[(209,1497),(575,1496)],floor=1502,weapon=(906,245)),
 dict(pelvis=(515,660),chest=(525,375),neck=(525,265),head=(532,150),shoulders=[(397,305),(615,310)],elbows=[(302,520),(704,520)],wrists=[(204,730),(810,730)],hips=[(440,692),(589,692)],knees=[(389,1010),(638,1010)],ankles=[(320,1432),(684,1425)],soles=[(328,1520),(713,1517)],floor=1526,weapon=None),
 dict(pelvis=(516,711),chest=(523,452),neck=(530,330),head=(526,166),shoulders=[(392,365),(630,365)],elbows=[(301,570),(721,580)],wrists=[(201,760),(810,762)],hips=[(440,757),(602,757)],knees=[(394,1045),(632,1047)],ankles=[(325,1440),(687,1434)],soles=[(327,1518),(721,1515)],floor=1525,weapon=None),
 dict(pelvis=(404,648),chest=(408,365),neck=(400,256),head=(400,145),shoulders=[(291,304),(489,310)],elbows=[(198,510),(578,525)],wrists=[(96,702),(698,708)],hips=[(329,686),(478,686)],knees=[(282,1010),(505,1014)],ankles=[(211,1422),(541,1422)],soles=[(217,1491),(581,1491)],floor=1497,weapon=(878,353)),
 dict(pelvis=(406,657),chest=(420,373),neck=(415,253),head=(420,147),shoulders=[(295,310),(505,312)],elbows=[(200,510),(602,525)],wrists=[(98,705),(706,710)],hips=[(331,694),(481,694)],knees=[(282,1019),(514,1020)],ankles=[(209,1440),(563,1435)],soles=[(215,1511),(603,1510)],floor=1517,weapon=(903,250)),
 dict(pelvis=(414,659),chest=(420,377),neck=(408,258),head=(409,146),shoulders=[(293,312),(503,317)],elbows=[(196,515),(598,530)],wrists=[(95,716),(711,724)],hips=[(337,699),(487,699)],knees=[(290,1025),(516,1025)],ankles=[(216,1432),(560,1432)],soles=[(222,1505),(605,1504)],floor=1512,weapon=(901,240)),
]

# Preserve the painted cuff and wrist. A closed hand replaces the open fingers
# on the three sword rigs, with the grip set a little behind the guard.
SWORD_GRIPS={0:((84,696),(915,320)),4:((93,696),(900,320)),5:((94,704),(896,312))}


def sub(a,b):return (a[0]-b[0],a[1]-b[1])
def length(a):return math.hypot(*a)
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def segment(p,a,b):
 d=sub(b,a);den=d[0]*d[0]+d[1]*d[1]
 t=max(0,min(1,((p[0]-a[0])*d[0]+(p[1]-a[1])*d[1])/den))
 return length(sub(p,(a[0]+t*d[0],a[1]+t*d[1]))),t
def path(p,points):
 best=(float('inf'),0);offset=0
 for a,b in zip(points,points[1:]):
  distance,t=segment(p,a,b);size=length(sub(b,a))
  if distance<best[0]:best=(distance,offset+t*size)
  offset+=size
 return best
def r(value):return round(value,5)


def make(hero,index):
 body_name=f'{hero}-body-v2' if index in (1,2,3,4) else f'{hero}-bind-v1'
 raw=ROOT/f'Assets/SpiritTide/Resources/SpiritTide/Art/Rig/{body_name}.png'
 im=Image.open(raw).convert('RGBA');alpha=im.getchannel('A');m=LANDMARKS[index].copy()
 wrist_cut=None
 if index in SWORD_GRIPS:
  wrist,m['weapon']=SWORD_GRIPS[index]
  m['wrists']=[wrist,m['wrists'][1]]
  wrist_cut=wrist[1]
 scale=192/(m['floor']-14)
 def point(p):return {'x':r((p[0]-m['pelvis'][0])*scale),'y':r((m['floor']-p[1])*scale)}
 bone=[m['pelvis'],m['chest'],m['neck'],m['neck']]
 for side in range(2):bone += [m['shoulders'][side],m['elbows'][side],m['wrists'][side]]
 for side in range(2):bone += [m['hips'][side],m['knees'][side],m['ankles'][side]]
 bone += [m['neck'],m['weapon'] or m['wrists'][0]]
 arms=[[m['shoulders'][i],m['elbows'][i],m['wrists'][i]] for i in range(2)]
 legs=[[m['hips'][i],m['knees'][i],m['ankles'][i]] for i in range(2)]
 def body_kind(p):
  if p[1]<m['neck'][1]+5:return 3
  choices=[]
  for side,points in enumerate(arms):
   distance,z=path(p,points);choices.append((distance/53,4+side*3))
  for side,points in enumerate(legs):
   distance,z=path(p,points);choices.append((distance/83+(5 if p[1]<m['hips'][side][1]-25 else 0),10+side*3))
  center=abs(p[0]-m['chest'][0])/113+(6 if p[1]>m['hips'][0][1]+35 else 0)
  score,kind=min(choices)
  return 0 if center<score else kind
 def classify(p):
  if m['weapon'] and p[0]>790:return 17
  return body_kind(p)
 def weights(p,kind):
  if kind==17:return 17,17,1.0
  if kind==16:return 3,16,1-smooth(m['neck'][1]-60,600,p[1])
  if kind==3:return 3,3,1.0
  if kind==0:
   if p[1]<m['chest'][1]:return 1,3,smooth(m['neck'][1]+15,m['chest'][1],p[1])
   return 0,1,smooth(m['chest'][1],m['pelvis'][1]-35,p[1])
  arm=kind in (4,7);side=(kind-4)//3 if arm else (kind-10)//3
  points=arms[side] if arm else legs[side];distance,z=path(p,points)
  l1=length(sub(points[1],points[0]))
  if z<l1*.22:return (1 if arm else 0),kind,1-smooth(0,l1*.22,z)
  if z<l1+25:return kind,kind+1,1-smooth(l1-25,l1+25,z)
  w=smooth(points[2][1]-(20 if arm else 40),points[2][1]+(22 if arm else 10),p[1])
  return kind+1,kind+2,1-w
 def group(kind):return {16:0,13:1,7:2,0:3,10:4,3:5,17:6,4:7}[kind]
 vertices=[];lookup={};triangles=[[] for _ in range(8)]
 def opacity(p):return alpha.getpixel((min(1023,max(0,int(p[0]))),min(1535,max(0,int(p[1])))))
 def vertex(x,y,kind):
  # Body joints share identical vertices across the complete skin surface.
  # Only the occluded back hair and the isolated weapon have independent edges.
  surface=kind if kind in (16,17) else 0
  key=(x,y,surface)
  if key in lookup:return lookup[key]
  binding=surface if surface else body_kind((x,y))
  a,b,w=weights((x,y),binding);i=len(vertices);lookup[key]=i
  vertices.append({'p':point((x,y)),'u':r(x/im.width),'v':r(1-y/im.height),'a':a,'b':b,'w':r(w)})
  return i
 def triangle(points):
  center=(sum(p[0] for p in points)/3,sum(p[1] for p in points)/3)
  sample=max([center,*points],key=opacity);kind=classify(sample if opacity(center)<32 else center)
  ids=[vertex(x,y,kind) for x,y in points];triangles[group(kind)].extend(ids)
 def cell(x,y,x1,y1):
  if wrist_cut is not None and x1<=152 and y>=wrist_cut and y<840:return
  if alpha.crop((x,y,x1,y1)).getextrema()[1]<16:return
  triangle([(x,y),(x1,y1),(x1,y)]);triangle([(x,y),(x,y1),(x1,y1)])
 step=8
 for y in range(0,im.height,step):
  for x in range(0,im.width,step):cell(x,y,min(im.width,x+step),min(im.height,y+step))
 out={'hero':index,'id':hero,'resource':f'SpiritTide/Art/Rig/{body_name}','source_width':1024,'source_height':1536,'height':192,'bones':[point(p) for p in bone],'soles':[point(p) for p in m['soles']],'vertices':vertices,'triangles':[i for group in triangles for i in group],'weapon':1 if index==3 else 2 if m['weapon'] else 0,'stride':1.0 if index in (0,1) else 1.13 if index==2 else 1.07 if index==3 else .96,'hair':index in (1,2,3,4)}
 out['hair_resource']='';out['hair_vertices']=[];out['hair_triangles']=[]
 if index in (1,2,3,4):
  name=f'{hero}-hair-v2';image=Image.open(ROOT/f'Assets/SpiritTide/Resources/SpiritTide/Art/Rig/{name}.png');a=image.getchannel('A')
  bbox=a.point(lambda v:255 if v>128 else 0).getbbox()
  desired={1:(158,18,592,655),2:(112,8,496,595),3:(45,195,335,515),4:(65,18,512,590)}[index]
  verts=[];indices=[];seen={}
  def hair_vertex(x,y):
   key=(x,y)
   if key in seen:return seen[key]
   target=(desired[0]+(x-bbox[0])*(desired[2]-desired[0])/(bbox[2]-bbox[0]),desired[1]+(y-bbox[1])*(desired[3]-desired[1])/(bbox[3]-bbox[1]))
   at=len(verts);seen[key]=at
   verts.append({'p':point(target),'u':r(x/image.width),'v':r(1-y/image.height),'a':3,'b':16,'w':r(1-smooth(m['neck'][1]-60,600,target[1]))})
   return at
  for y in range(0,image.height,12):
   for x in range(0,image.width,12):
    x1=min(image.width,x+12);y1=min(image.height,y+12)
    if a.crop((x,y,x1,y1)).getextrema()[1]<16:continue
    ids=[hair_vertex(*q) for q in [(x,y),(x1,y),(x1,y1),(x,y1)]]
    indices.extend([ids[0],ids[2],ids[1],ids[0],ids[3],ids[2]])
  out['hair_resource']=f'SpiritTide/Art/Rig/{name}';out['hair_vertices']=verts;out['hair_triangles']=indices
 assert len(out['bones'])==18 and len(vertices)<18000 and len(out['triangles'])%3==0
 return out


def main():
 rigs=[make(hero,i) for i,hero in enumerate(HEROES)]
 (DATA/'motion_rigs.json').write_text(json.dumps({'schema':1,'rigs':rigs},separators=(',',':'))+'\n',encoding='utf-8')
 index={'version':'0.11.0','bones_per_hero':18,'source':'Tools/prepare_motion_rigs.py','png_pixels_modified':False,'rigs':[{'id':d['id'],'vertices':len(d['vertices']),'triangles':len(d['triangles'])//3,'resource':d['resource'],'hair_vertices':len(d['hair_vertices']),'hair_triangles':len(d['hair_triangles'])//3,'hair_resource':d['hair_resource']} for d in rigs]}
 (ROOT/'Docs/motion-rig-index.json').write_text(json.dumps(index,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(index))


if __name__=='__main__':main()
