import bpy, math, json, random
from pathlib import Path
from mathutils import Vector
root=Path(r'C:/Users/Mompas/Documents/GitHub/leftovers/leftovers')
source=root/'ArtSource/NightKitchen'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.name='LEFTOVERS - 3 AM kitchen'
objects=[]; materials={}; specs=[]
def material(name,color,rough=.6,metal=0,glow=False):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 materials[name]=m;specs.append(dict(name=name,color=list(color),smoothness=1-rough,metallic=metal,unlit=glow))
material('Night plaster',(.28,.32,.32),.9)
material('Ceiling plaster',(.36,.39,.40),.95)
material('Trim wood',(.20,.12,.075),.56)
material('Door oak',(.33,.21,.12),.55)
material('Door inset',(.28,.17,.09),.62)
material('Dining oak',(.34,.20,.105),.4)
material('Dining edges',(.24,.135,.068),.48)
material('Aged steel',(.46,.51,.52),.3,.5)
material('Pale enamel',(.64,.69,.67),.32,.08)
material('Porcelain',(.71,.73,.66),.32)
material('Graphite',(.027,.036,.041),.48,.1)
material('Oven glass',(.018,.032,.040),.18,.2)
material('Warm grout',(.25,.26,.24),.85)
for i in range(4): material('Ceramic tile '+str(i),(.42+i*.018,.44+i*.014,.41+i*.011),.38)
material('Counter stone',(.22,.23,.21),.42)
material('Sink inner',(.30,.36,.37),.25,.55)
material('Window paint',(.34,.39,.39),.48)
material('Blind fabric',(.115,.17,.20),.94)
material('Towel blue',(.16,.23,.27),.96)
material('Rug charcoal',(.17,.15,.17),.97)
material('Rug stripe',(.27,.21,.24),.98)
material('Plant leaves',(.13,.24,.12),.75)
material('Plant earth',(.065,.045,.028),1)
material('Terracotta',(.37,.19,.095),.8)
material('Mustard',(.60,.40,.10),.5)
material('Paper',(.69,.64,.46),.9)
material('Night sky',(.035,.065,.115),1,0,True)
material('Distant roof',(.013,.024,.036),1,0,True)
material('Moon',(.60,.78,.92),1,0,True)
material('Lamp glass',(.77,.60,.35),.3,0,True)
material('Ceiling glass',(.43,.47,.47),.4)
def pos(v): return (-v[0],-v[2],v[1])
def finish(o,name,mat,group,solid=False,bevel=0):
 o.name=name;o.data.materials.append(materials[mat]);o['group']=group;o['solid']=solid
 bpy.context.view_layer.objects.active=o
 if bevel:
  b=o.modifiers.new('Soft edges','BEVEL');b.width=bevel;b.segments=2
  bpy.ops.object.modifier_apply(modifier=b.name)
  n=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=40
  bpy.ops.object.modifier_apply(modifier=n.name)
 objects.append(o);return o

def box(name,p,size,mat,group='Room',bevel=.012,solid=False,angle=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos(p));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.rotation_euler.z=-math.radians(angle)
 return finish(o,name,mat,group,solid,bevel)
def cylinder(name,p,r,depth,mat,group='Details',vertices=20,front=False):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=pos(p));o=bpy.context.object
 if front:o.rotation_euler.x=math.pi/2
 return finish(o,name,mat,group,False,.006)
def sphere(name,p,scale,mat,group='Details',sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=pos(p));o=bpy.context.object;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,name,mat,group)
def tube(name,points,r,mat,group='Details'):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=r;curve.bevel_resolution=1;curve.resolution_u=8
 spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
 for p,v in zip(spline.points,points):p.co=(*pos(v),1)
 o=bpy.data.objects.new(name,curve);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object
 return finish(o,name,mat,group)
def bowl(name,p,r,height,mat,group='Details'):
 verts=[];faces=[];n=20
 for radius,h in [(r*.50,0),(r,.82*height),(r,height),(r-.025,height),(r*.48,.035)]:
  for i in range(n):
   a=i*2*math.pi/n;verts.append(pos((p[0]+radius*math.cos(a),p[1]+h,p[2]+radius*math.sin(a))))
 for j in range(4):
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 faces.append(tuple(range(4*n,5*n)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o)
 return finish(o,name,mat,group)
# Shell and actual opening for the night window.
box('Back wall left',(4.9,2.35,-.70),(3.1,4.7,.22),'Night plaster',solid=True)
box('Back wall right',(-3.0,2.35,-.70),(6.7,4.7,.22),'Night plaster',solid=True)
box('Window wall below',(1.85,1.07,-.70),(3.0,2.14,.22),'Night plaster',solid=True)
box('Window wall above',(1.85,4.40,-.70),(3.0,.60,.22),'Night plaster',solid=True)
box('Left wall',(6.46,2.35,3.25),(.22,4.7,8.1),'Night plaster',solid=True)
box('Right wall',(-6.46,2.35,3.25),(.22,4.7,8.1),'Night plaster',solid=True)
box('Fourth wall',(0,2.35,7.40),(13.1,4.7,.22),'Night plaster',solid=True)
box('Ceiling',(0,4.76,3.25),(13.1,.14,8.3),'Ceiling plaster',solid=True)
box('Floor shadow base',(0,-.055,3.15),(13,.08,8.6),'Graphite',solid=True)
for x in [-6.32,6.32]:box('Side skirting',(x,.095,3.3),(.08,.19,8.1),'Trim wood')
box('Back skirting',(0,.095,-.55),(12.7,.19,.08),'Trim wood')
box('Front skirting',(0,.095,7.25),(12.7,.19,.08),'Trim wood')
# Closed paneled side doors, each complete in its frame.
for x,z in [(6.28,2.2),(-6.28,2.8)]:
 side=1 if x>0 else -1; group='Closed side doors'
 box('Closed door',(x,1.65,z),(.09,3.3,1.6),'Door oak',group,.025,True)
 for y,h in [(1.05,1.6),(2.55,1.05)]:box('Raised door panel',(x-side*.055,y,z),(.035,h,1.24),'Door inset',group,.025)
 for dz in [-.88,.88]:box('Door frame',(x-side*.035,1.72,z+dz),(.18,3.44,.13),'Trim wood',group,.016)
 box('Door lintel',(x-side*.035,3.44,z),(.18,.14,1.90),'Trim wood',group,.015)
 tube('Door lever',[(x-side*.13,1.55,z+.57),(x-side*.23,1.55,z+.57),(x-side*.23,1.55,z+.34)],.025,'Aged steel',group)
# Window: four panes, thick sill and a rolled fabric blind.
for x in [.35,3.35]:box('Window outer jamb',(x,3.13,-.51),(.12,2.08,.19),'Window paint','Window',.015,True)
for y in [2.12,4.15]:box('Window frame rail',(1.85,y,-.51),(3.12,.12,.19),'Window paint','Window',.012,True)
box('Window center mullion',(1.85,3.13,-.45),(.075,1.96,.11),'Window paint','Window',.007)
box('Window cross rail',(1.85,3.12,-.45),(2.92,.075,.11),'Window paint','Window',.007)
box('Wide window sill',(1.85,2.08,-.38),(3.25,.10,.44),'Window paint','Window',.015)
box('Roman blind',(1.85,4.06,-.30),(3.12,.42,.10),'Blind fabric','Window',.018)
for y in [3.90,4.04,4.18]:box('Blind folds',(1.85,y,-.23),(3.12,.038,.055),'Blind fabric','Window',.008)
box('Night outside',(0,3,-4.4),(18,9,.1),'Night sky','Night beyond window',0)
sphere('Full moon',(.97,3.72,-3.0),(.16,.16,.07),'Moon','Night beyond window',2)
for x,h in [(-1.1,1.9),(.2,1.5),(2.9,1.65),(4.1,2.0)]:
 box('Distant house',(x,h/2,-3.4),(1.3,h,.35),'Distant roof','Night beyond window',0)
 verts=[pos((x-.8,h,-3.2)),pos((x+.8,h,-3.2)),pos((x,h+.65,-3.2))];mesh=bpy.data.meshes.new('Roof');mesh.from_pydata(verts,[],[(0,1,2)]);mesh.update();o=bpy.data.objects.new('Roof silhouette',mesh);scene.collection.objects.link(o);finish(o,o.name,'Distant roof','Night beyond window')
# Backsplash tiles with recessed grout.
box('Backsplash grout',(.15,2.03,-.52),(7.35,.82,.05),'Warm grout','Backsplash',0)
for row in range(3):
 for col in range(27):
  x=-3.45+col*.27;y=1.78+row*.26
  if .3<x<3.4 and y>2.12:continue
  box('Ceramic square',(x,y,-.476),(.259,.249,.03),'Ceramic tile '+str((row*7+col*3)%4),'Backsplash',.009)
# Sink counter, made with a real cutout and inset bowl.
slab=box('Sink countertop',(1.8,1.69,.03),(3.6,.075,1.28),'Counter stone','Sink',.025,True)
cut=box('Sink cutout',(1.8,1.7,.04),(1.48,.6,.83),'Graphite','Sink',.10)
bpy.context.view_layer.objects.active=slab;mod=slab.modifiers.new('Basin opening','BOOLEAN');mod.object=cut;mod.operation='DIFFERENCE';bpy.ops.object.modifier_apply(modifier=mod.name);objects.remove(cut);bpy.data.objects.remove(cut,do_unlink=True)
box('Sink bottom',(1.8,1.46,.04),(1.30,.04,.66),'Sink inner','Sink',.065)
for x in [1.10,2.50]:box('Sink basin side',(x,1.57,.04),(.065,.25,.77),'Aged steel','Sink',.025)
for z in [-.35,.43]:box('Sink basin end',(1.8,1.57,z),(1.42,.25,.065),'Aged steel','Sink',.025)
for x in [1.04,2.56]:box('Sink rim',(x,1.733,.04),(.065,.035,.91),'Aged steel','Sink',.018)
for z in [-.405,.485]:box('Sink rim',(1.8,1.733,z),(1.58,.035,.065),'Aged steel','Sink',.018)
cylinder('Drain',(1.8,1.484,.04),.075,.008,'Graphite','Sink')
faucet=[(1.8,1.73,-.42),(1.8,2.06,-.42),(1.8,2.17,-.38),(1.8,2.22,-.29),(1.8,2.19,-.19),(1.8,2.10,-.16)]
tube('Curved faucet',faucet,.036,'Aged steel','Sink')
for x in [1.52,2.08]:cylinder('Tap base',(x,1.76,-.40),.062,.055,'Aged steel','Sink');box('Tap lever',(x,1.81,-.39),(.15,.025,.045),'Aged steel','Sink',.008)
# Oven with inset glass, burner rings, knobs, and folded tea towel.
group='Oven';ox=-.88
box('Oven body',(ox,.87,.045),(1.54,1.74,1.23),'Pale enamel',group,.045,True)
box('Oven black plinth',(ox,.09,.08),(1.46,.15,1.15),'Graphite',group,.015)
box('Cooktop',(ox,1.76,.05),(1.58,.07,1.25),'Aged steel',group,.022)
box('Oven control panel',(ox,1.52,.687),(1.48,.28,.07),'Pale enamel',group,.016)
box('Oven door border',(ox,.79,.694),(1.39,1.12,.08),'Aged steel',group,.035)
box('Oven inset glass',(ox,.83,.744),(1.17,.76,.018),'Oven glass',group,.035)
box('Oven inner reflection',(ox,.86,.756),(1.02,.62,.008),'Graphite',group,.018)
box('Oven handle',(ox,1.31,.80),(1.22,.055,.07),'Graphite',group,.022)
for dx in [-.53,-.18,.18,.53]:cylinder('Control dial',(ox+dx,1.53,.765),.075,.055,'Graphite',group,16,True)
for dx in [-.38,.38]:
 for z in [-.26,.34]:
  cylinder('Burner',(ox+dx,1.807,z),.22,.025,'Graphite',group,24)
  cylinder('Burner center',(ox+dx,1.824,z),.105,.025,'Aged steel',group,20)
box('Oven splash guard',(ox,1.91,-.49),(1.52,.24,.075),'Pale enamel',group,.02)
box('Hanging folded towel',(ox,1.02,.85),(.46,.57,.045),'Towel blue',group,.014)
for dx in [-.16,.16]:box('Towel seam',(ox+dx,1.02,.879),(.012,.52,.008),'Window paint',group,.002)
box('Range hood',(ox,2.95,-.12),(1.83,.19,.83),'Aged steel','Range hood',.035)
box('Hood shadow',(ox,2.84,-.10),(1.54,.035,.65),'Graphite','Range hood',.01)
# Table and matching chairs, softly bevelled furniture.
group='Dining furniture';tx=3.65;tz=4.75
box('Dining tabletop',(tx,1.28,tz),(2.95,.12,1.85),'Dining oak',group,.045,True)
for k in range(4):box('Tabletop board seam',(tx,1.343,tz-.68+k*.45),(2.80,.003,.009),'Dining edges',group,.001)
for dx in [-1.24,1.24]:
 for dz in [-.70,.70]:box('Table leg',(tx+dx,.61,tz+dz),(.13,1.22,.13),'Dining edges',group,.018,True)
for dz in [-.72,.72]:box('Table apron',(tx,1.13,tz+dz),(2.5,.20,.07),'Dining edges',group,.01)
def chair(cx,cz,angle):
 before=len(objects)
 box('Chair seat',(cx,.77,cz),(.78,.09,.78),'Dining oak',group,.035,True)
 for dx in [-.30,.30]:
  for dz in [-.28,.28]:box('Chair leg',(cx+dx,.37,cz+dz),(.085,.74,.085),'Dining edges',group,.012,True)
 for dx in [-.30,.30]:box('Chair upright',(cx+dx,1.17,cz+.30),(.09,.9,.085),'Dining edges',group,.012)
 box('Chair back',(cx,1.35,cz+.31),(.69,.48,.055),'Dining oak',group,.025,True)
 for o in objects[before:]:
  center=Vector(pos((cx,0,cz)));v=o.location-center;a=math.radians(-angle);o.location=center+Vector((v.x*math.cos(a)-v.y*math.sin(a),v.x*math.sin(a)+v.y*math.cos(a),v.z));o.rotation_euler.z+=a
chair(2.1,4.7,90);chair(3.8,6.0,0);chair(3.9,3.5,180)
# Woven runner on the reclaimed plank floor.
box('Kitchen rug',(0.35,.022,1.95),(5.35,.024,1.10),'Rug charcoal','Rug',.016)
for i in range(5):box('Woven rug stripes',(.35,.036,1.51+i*.21),(5.05,.003,.10),'Rug stripe','Rug',.001)
for x in [-2.23,2.93]:box('Rug stitched border',(x,.037,1.95),(.045,.002,.98),'Door oak','Rug',.001)
# Small countertop silhouettes, with restrained detail.
bowl('Plant pot',(3.35,1.73,.02),.18,.24,'Terracotta','Countertop details');cylinder('Plant soil',(3.35,1.92,.02),.15,.02,'Plant earth')
for i in range(7):
 a=i*2.4;leaf=sphere('Broad plant leaf',(3.35+math.cos(a)*.13,2.11+(i%3)*.055,.02+math.sin(a)*.10),(.075,.22,.04),'Plant leaves');leaf.rotation_euler.y=.5*math.sin(a)
cylinder('Dish soap',(2.88,1.91,-.31),.085,.32,'Mustard');cylinder('Soap cap',(2.88,2.09,-.31),.037,.06,'Graphite')
box('Dish rack',(.47,1.78,.02),(.65,.10,.66),'Window paint','Countertop details',.018)
for z in [-.21,.0,.21]:
 cylinder('Rack plate',(.47,1.98,z),.18,.028,'Porcelain','Countertop details',24,True)
for x in [.16,.78]:tube('Rack side',[(x,1.8,-.29),(x,2.06,-.29),(x,2.06,.31),(x,1.8,.31)],.012,'Aged steel')
box('Knife block',(-2.05,1.91,-.22),(.25,.42,.23),'Dining oak','Countertop details',.025)
for x in [-2.12,-2.04,-1.97]:box('Knife handle',(x,2.19,-.22),(.045,.27,.06),'Graphite','Countertop details',.015)
box('Small toaster',(-3.04,1.92,.16),(.58,.43,.39),'Graphite','Countertop details',.075)
for x in [-3.17,-2.93]:box('Toaster slot',(x,2.138,.16),(.11,.008,.27),'Aged steel','Countertop details',.018)
box('Cutting board',(-3.45,2.02,-.35),(.31,.68,.045),'Porcelain','Countertop details',.055)
box('Food carton',(-2.45,1.95,-.22),(.28,.49,.21),'Mustard','Countertop details',.01)
box('Carton label',(-2.45,1.97,-.105),(.20,.27,.012),'Paper','Countertop details',.006)
bowl('Bowl on microwave',(4.55,2.84,.04),.26,.17,'Porcelain','Countertop details')
cylinder('Waste bin',(5.68,.39,1.20),.28,.74,'Blind fabric','Waste bin',12)
bowl('Bin rim',(5.68,.74,1.20),.30,.065,'Graphite','Waste bin')
# Ceiling fixture is off. Fourth-wall lamp provides a restrained warm edge.
cylinder('Ceiling light mount',(1.0,4.62,2.0),.32,.10,'Graphite','Lamps',24)
sphere('Ceiling frosted dome',(1.0,4.56,2.0),(.28,.15,.28),'Ceiling glass','Lamps',2)
box('Wall lamp backplate',(-1.2,3.06,7.23),(.22,.40,.06),'Graphite','Lamps',.028)
box('Wall lamp shade',(-1.2,3.16,7.04),(.39,.40,.28),'Lamp glass','Lamps',.04)
# Keep an editable Blender source and export meshes using the same conversion as the microwave.
bpy.context.view_layer.update();records=[]
for o in objects:
 mesh=o.data;mesh.calc_loop_triangles();vertices=[];normals=[];triangles=[]
 for tri in mesh.loop_triangles:
  for li in reversed(tri.loops):
   loop=mesh.loops[li];v=o.matrix_world@mesh.vertices[loop.vertex_index].co;n=o.matrix_world.to_3x3()@mesh.corner_normals[li].vector;n.normalize()
   vertices.extend([-v.x,v.z,-v.y]);normals.extend([-n.x,n.z,-n.y]);triangles.append(len(triangles))
 records.append(dict(name=o.name,group=o['group'],material=mesh.materials[0].name,solid=o['solid'],vertices=vertices,normals=normals,triangles=triangles))
(source/'NightKitchenMeshes.json').write_text(json.dumps(dict(materials=specs,meshes=records)))
bpy.ops.wm.save_as_mainfile(filepath=str(source/'NightKitchen.blend'))
print('NIGHT_KITCHEN_READY',len(objects),'objects',sum(len(r['triangles'])//3 for r in records),'triangles')
