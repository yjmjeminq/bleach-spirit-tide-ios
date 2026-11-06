"""Read atlas alpha to describe sprite rectangles. Does not modify any PNG pixels."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy import ndimage
ROOT=Path(__file__).resolve().parent.parent
atlases=[]
for p in sorted((ROOT/'Assets/SpiritTide/Resources/SpiritTide/Art/Experience').glob('*_motion_v1.png')):
    with Image.open(p) as im:alpha=np.asarray(im.getchannel('A'));w,h=im.size
    cells=[];cw,ch=w//3,h//3
    for cell in range(9):
        left,top=cell%3*cw,cell//3*ch
        mask=alpha[top:top+ch,left:left+cw]>128
        labels,n=ndimage.label(mask)
        areas=np.bincount(labels.ravel());areas[0]=0;owner=areas.argmax();ys,xs=np.where(labels==owner)
        assert areas[owner]>1500
        x0=max(0,int(xs.min())-3);x1=min(cw,int(xs.max())+4);y0=max(0,int(ys.min())-3);y1=min(ch,int(ys.max())+4)
        cells.append({'cell':cell,'x':left+x0,'y':top+y0,'w':x1-x0,'h':y1-y0,'pivot_x':float((cw*.5-x0)/(x1-x0))})
    atlases.append({'key':p.stem,'width':w,'height':h,'cells':cells})
out=ROOT/'Assets/SpiritTide/Resources/SpiritTide/Data/motion_regions.json'
out.write_text(json.dumps({'atlases':atlases},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'atlases':len(atlases),'frames':sum(len(x['cells']) for x in atlases),'pngs_edited':0}))
