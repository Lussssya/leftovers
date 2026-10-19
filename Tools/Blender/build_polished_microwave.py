import bpy,math,json
from pathlib import Path
from mathutils import Vector
root=Path('C:/Users/Mompas/Documents/GitHub/leftovers/leftovers')
folder=root/'Assets/Models/Microwave/Polished'; source=root/'ArtSource/Microwave'
scene=bpy.data.scenes.new('LEFTOVERS - polished microwave'); bpy.context.window.scene=scene
materials={}; objects=[]
def material(name,color,rough=.35,metal=0,alpha=1,texture=None,emission=False):
 m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.diffuse_color=(*color,alpha);m.use_nodes=True
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(*color,alpha);bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal;bs.inputs['Alpha'].default_value=alpha
 if texture:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(folder/texture));m.node_tree.links.new(t.outputs['Color'],bs.inputs['Base Color'])
  if name=='FineScreen':m.node_tree.links.new(t.outputs['Alpha'],bs.inputs['Alpha'])
  if emission:m.node_tree.links.new(t.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.8
 if alpha<1 or name=='FineScreen':
  prop=m.bl_rna.properties.get('surface_render_method')
  if prop:m.surface_render_method=next(i.identifier for i in prop.enum_items if i.identifier=='DITHERED')
 materials[name]=m;return m
material('Enamel',(.69,.78,.82),.27,.08);material('DoorEnamel',(.76,.83,.85),.26,.06)
material('Interior',(.77,.76,.67),.46);material('Seal',(.025,.035,.043),.7);material('Bezel',(.10,.14,.17),.3,.16)
material('Steel',(.31,.38,.40),.26,.45);material('Turntable',(.44,.54,.55),.2,.22);material('Handle',(.79,.85,.87),.24,.08)
material('FineScreen',(.1,.13,.15),.32,texture='FineScreen.png');material('Keypad',(.1,.1,.1),.48,texture='Keypad.png')
material('Timer',(.05,.05,.05),.4,texture='RedTimerAtlas.png',emission=True);material('Lamp',(.95,.83,.57),.4)
def finish(o,name,mat,group):
 o.name=name;o.data.materials.clear();o.data.materials.append(materials[mat]);o['part']=group;objects.append(o);return o
def bevel(o,width,segments=2):
 mod=o.modifiers.new('Soft manufactured edges','BEVEL');mod.width=width;mod.segments=segments
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in o.data.polygons:p.use_smooth=False
 return o
def box(name,loc,size,mat='Enamel',bevel_size=.006,group='body'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel_size:bevel(o,bevel_size,3 if bevel_size>.015 else 2)
 return finish(o,name,mat,group)
def rect(cx,cz,w,h,r,steps=5):
 pts=[]
 for x,z,start in [(cx+w/2-r,cz+h/2-r,0),(cx-w/2+r,cz+h/2-r,90),(cx-w/2+r,cz-h/2+r,180),(cx+w/2-r,cz-h/2+r,270)]:
  for i in range(steps+1):
   a=math.radians(start+i*90/steps);pts.append((x+r*math.cos(a),z+r*math.sin(a)))
 return pts
def ring(name,outer,inner,front,back,mat,group):
 a=rect(*outer);b=rect(*inner);n=len(a);verts=[(x,y,z) for y,loop in [(front,a),(front,b),(back,a),(back,b)] for x,z in loop];faces=[]
 for i in range(n):
  j=(i+1)%n;faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i),(2*n+i,3*n+i,3*n+j,2*n+j)])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o)
 bpy.context.view_layer.objects.active=o;o.select_set(True);bevel(o,.0015,1);o.select_set(False)
 return finish(o,name,mat,group)
def panel(name,cx,cz,w,h,r,y,mat,group,uvscale=(1,1),uvoffset=(0,0)):
 pts=rect(cx,cz,w,h,r);mesh=bpy.data.meshes.new(name);mesh.from_pydata([(x,y,z) for x,z in pts],[],[tuple(range(len(pts)))]);mesh.update();uv=mesh.uv_layers.new()
 for loop in mesh.loops:
  x,_,z=mesh.vertices[loop.vertex_index].co;uv.data[loop.index].uv=((x-cx+w/2)/w*uvscale[0]+uvoffset[0],(z-cz+h/2)/h*uvscale[1]+uvoffset[1])
 o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);return finish(o,name,mat,group)
def cyl(name,loc,r,depth,mat,group='body',vertices=48):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc);o=bpy.context.object;bevel(o,.0015,1);return finish(o,name,mat,group)
# A hollow shell with separate interior walls and a softly faceted outer casing.
shell=box('Unified enamel casing',(0,0,.228),(.66,.43,.406),'Enamel',.020)
cutter=box('Cavity cutter',(-.055,-.07,.234),(.48,.50,.298),'Interior',.016)
bpy.context.view_layer.objects.active=shell
cut=shell.modifiers.new('Open interior','BOOLEAN');cut.object=cutter;cut.operation='DIFFERENCE'
bpy.ops.object.modifier_apply(modifier=cut.name)
objects.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)
ring('Front aperture',(0,.228,.646,.381,.018),(-.055,.234,.477,.298,.018),-.212,-.194,'DoorEnamel','body')
box('Cavity floor',(-.055,.012,.067),(.48,.36,.018),'Interior',.009)
box('Cavity ceiling',(-.055,.012,.392),(.48,.36,.017),'Interior',.009)
box('Cavity left',(-.295,.012,.232),(.014,.36,.32),'Interior',.007)
box('Cavity right',(.186,.012,.232),(.014,.36,.32),'Interior',.007)
box('Cavity back',(-.055,.183,.232),(.48,.018,.32),'Interior',.012)
ring('Back wall emboss',(-.055,.236,.412,.25,.018),(-.055,.236,.398,.236,.014),.169,.174,'Interior','body')
for x in [-.25,.25]:
 for y in [-.15,.15]:cyl('Rubber foot', (x,y,.019),.034,.027,'Seal',vertices=12)
for i in range(9):box('Side vent '+str(i),(-.331,.052+i*.012,.28),(.001,.004,.075),'Seal',.001)
for i in range(7):box('Interior vent '+str(i),(-.286,.04+i*.013,.26),(.001,.004,.09),'Bezel',.0004)
# Door surround, inset reveal, fine perforated glazing and continuous curved handle.
ring('Door outer frame',(-.055,.228,.524,.381,.019),(-.079,.232,.418,.288,.014),-.238,-.212,'DoorEnamel','door')
ring('Window chamfer',(-.079,.232,.43,.300,.016),(-.079,.232,.397,.266,.012),-.241,-.230,'Bezel','door')
ring('Inner door seal',(-.055,.228,.503,.358,.014),(-.079,.232,.402,.271,.010),-.208,-.202,'Seal','door')
panel('Perforated smoked window',-.079,.232,.397,.266,.012,-.233,'FineScreen','door',(10,7.65))
# Four rectangular sections flow smoothly from their mounts into the vertical grip.
path=[(.179,-.238,.097),(.179,-.259,.103),(.179,-.278,.12),(.179,-.282,.145),(.179,-.282,.31),(.179,-.278,.337),(.179,-.259,.354),(.179,-.238,.360)]
verts=[];faces=[]
for x,y,z in path:
 for dx,dy in [(-.009,-.007),(.009,-.007),(.013,-.003),(.013,.003),(.009,.007),(-.009,.007),(-.013,.003),(-.013,-.003)]:verts.append((x+dx,y+dy,z))
for j in range(len(path)-1):
 for i in range(8):faces.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
faces.extend([tuple(reversed(range(8))),tuple(range((len(path)-1)*8,len(path)*8))]);mesh=bpy.data.meshes.new('Handle');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Sculpted handle',mesh);scene.collection.objects.link(o);finish(o,'Sculpted handle','Handle','door')
for z in [.105,.35]:box('Hinge',(-.307,-.216,z),(.013,.020,.032),'Steel',.003)
for z in [.13,.33]:box('Door latch',(.196,-.196,z),(.012,.022,.021),'Seal',.002,'door')
box('Control fascia',(.265,-.215,.228),(.108,.016,.375),'DoorEnamel',.009)
panel('Keypad surface',.265,.197,.085,.260,.004,-.224,'Keypad','body')
ring('Display rim',(.265,.36,.088,.047,.005),(.265,.36,.077,.035,.003),-.226,-.221,'Bezel','body')
panel('Flat red display',.265,.36,.077,.034,.002,-.2245,'Timer','display',(.1,.1),(.5,.9))
cyl('Turntable glass',(-.055,.006,.081),.150,.008,'Turntable','tray',64)
cyl('Turntable inset',(-.055,.006,.0855),.137,.001,'Turntable','tray',64)
cyl('Roller support',(-.055,.006,.074),.115,.008,'Seal','body',32)
box('Lamp diffuser',(.175,-.045,.33),(.008,.075,.048),'Lamp',.004,'lamp')
# Correct normals before exporting editable meshes.
import bmesh
for o in objects:
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
# Flat fronts face toward negative Y; ring winding and panel normals are checked explicitly.
for o in objects:
 if len(o.data.polygons)==1 and o.data.polygons[0].normal.y>0:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free()
hinge=bpy.data.objects.new('Door hinge - rotate Z to open',None);scene.collection.objects.link(hinge);hinge.location=(-.307,-.212,.228)
tray_pivot=bpy.data.objects.new('Turntable pivot',None);scene.collection.objects.link(tray_pivot);tray_pivot.location=(-.055,.006,.086)
bpy.context.view_layer.update()
for o in objects:
 if o['part'] in ['door','tray']:
  world_matrix=o.matrix_world.copy();o.parent=hinge if o['part']=='door' else tray_pivot;o.matrix_world=world_matrix
bpy.context.view_layer.update()
records=[]
for o in objects:
 mesh=o.data;mesh.calc_loop_triangles();positions=[];normals=[];uvs=[];triangles=[]
 for tri in mesh.loop_triangles:
  for li in reversed(tri.loops):
   loop=mesh.loops[li];v=o.matrix_world@mesh.vertices[loop.vertex_index].co;n=o.matrix_world.to_3x3()@mesh.corner_normals[li].vector
   positions.extend([-v.x,v.z,-v.y]);normals.extend([-n.x,n.z,-n.y]);uvs.extend(list(mesh.uv_layers.active.data[li].uv) if mesh.uv_layers.active else [0,0]);triangles.append(len(triangles))
 records.append(dict(name=o.name,part=o['part'],material=mesh.materials[0].name,vertices=positions,normals=normals,uv=uvs,triangles=triangles))
(folder/'MicrowaveMeshes.json').write_text(json.dumps({'meshes':records}))
# Studio presentation in the editable source file.
world=bpy.data.worlds.new('Studio');scene.world=world;world.use_nodes=True;next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.12,.15,.19,1)
def aim(o,point):o.rotation_euler=(Vector(point)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(.92,-1.5,.81));camera=bpy.context.object;camera.name='Presentation camera';aim(camera,(0,0,.22));camera.data.type='ORTHO';camera.data.ortho_scale=.96;scene.camera=camera
for name,pos,power,size in [('Key',(-.7,-.8,1.3),55,1),('Fill',(.8,-.5,.7),28,.8),('Rim',(0,.65,1),65,.7)]:
 bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.name=name;light.data.energy=power;light.data.shape='DISK';light.data.size=size;aim(light,(0,0,.2))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.001));floor=bpy.context.object;floor.name='Studio floor';floor.data.materials.append(material('StudioFloor',(.12,.16,.20),.7))
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_distance=1.1;area.spaces.active.region_3d.view_location=(0,0,.22);area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion();area.spaces.active.shading.type='MATERIAL'
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(source/'Microwave_Polished.blend'))
scene.render.filepath=str(source/'Microwave_closed.png');bpy.ops.render.render(write_still=True)
hinge.rotation_euler.z=math.radians(-105)
bpy.ops.object.light_add(type='POINT',location=(.13,-.05,.33));interior_light=bpy.context.object;interior_light.name='Preview interior light';interior_light.data.energy=.8;interior_light.data.color=(1,.81,.51);interior_light.data.shadow_soft_size=.045
scene.render.filepath=str(source/'Microwave_open.png');bpy.ops.render.render(write_still=True)
hinge.rotation_euler.z=0;interior_light.data.energy=0
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
hinge.select_set(True);tray_pivot.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(source/'Microwave_Polished.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'Microwave_Polished.blend'))
print('POLISHED_MODEL_READY' ,len(objects),'objects',sum(len(o.data.polygons) for o in objects),'polygons')

