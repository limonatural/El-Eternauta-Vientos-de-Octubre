#!/usr/bin/env python3
# Genera Assets/EternautaBeta/Scripts/Data/MapasGenerados.cs
# Mapa 1: Avellaneda (calles de la foto satelital, enderezado: Alsina al sur, Berutti al norte,
#         Av. Belgrano al oeste, Ing. Marconi al este, Av. Mitre en diagonal junto a la Plaza Alsina).
# Mapa 2: Puente Pueyrredon + Av. 9 de Julio con el Obelisco (Capitulo 2).
# Uso: python3 tools/generar_mapas.py
import os, random, math, sys
from collections import deque

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SALIDA = os.path.join(ROOT, 'Assets/EternautaBeta/Scripts/Data/MapasGenerados.cs')

def f(v): return ('%.2f' % v).rstrip('0').rstrip('.') + 'f' if '.' in ('%.2f' % v) else '%.1ff' % v
def q(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'

class Mapa:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.g = [['N'] * w for _ in range(h)]
        self.zona = [['0'] * w for _ in range(h)]
        self.ents = []        # (id, codigo C#)
        self.luces = []
        self.spawns = []
        self.lugares = []
        self.escenarios = []  # (id, nombre, ubicacion, descripcion)
        self.postales = []
        self.usados = set()
    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h: self.g[y][x] = c
    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h: return self.g[y][x]
        return 'N'
    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): self.set(x, y, c)
    def zrect(self, x0, y0, x1, y1, z):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if 0 <= x < self.w and 0 <= y < self.h: self.zona[y][x] = z
    def nid(self, base):
        i = base
        while i in self.usados: i += 1
        self.usados.add(i)
        return i
    def deco(self, sprite, x, y, ang=0, solido=True, radio=0.3, base=1000):
        i = self.nid(base)
        self.ents.append('D(%d, %s, %s, %s, %s, %s, %s)' % (i, q(sprite), f(x), f(y), f(ang), 'true' if solido else 'false', f(radio)))
        return i
    def cont(self, sprite, x, y, ang, tipo, tabla, fijo=None, completa=0, nombre=None, base=200, texto=None):
        i = self.nid(base)
        self.ents.append('C(%d, %s, %s, %s, %s, TipoEntidad.%s, %s, %s, %d, %s, %s)' % (
            i, q(sprite), f(x), f(y), f(ang), tipo, q(tabla) if tabla else 'null', q(fijo) if fijo else 'null', completa,
            q(nombre) if nombre else 'null', q(texto) if texto else 'null'))
        return i
    def luz(self, x, y, h, r, g, b, radio):
        self.luces.append('L(%s, %s, %s, %s, %s, %s, %s)' % (f(x), f(y), f(h), f(r), f(g), f(b), f(radio)))

CAMINABLE = set('.:p,;=dD')
def caminable(c): return c in CAMINABLE

# =====================================================================
# MAPA 1: AVELLANEDA
# =====================================================================
def avellaneda():
    W, HP = 144, 96
    ANX, ANY = 100, 98          # esquina del departamento (fuera del mapa jugable)
    HT = 112
    m = Mapa(W, HT)
    rnd = random.Random(20261029)
    for y in range(HT):
        for x in range(W): m.g[y][x] = '#'
    # --- terreno jugable ---
    for y in range(2, 94):
        for x in range(2, 142): m.g[y][x] = 'N'

    def mitre_c(y): return 101 + (y - 42) * 0.15
    def es_calle(x, y):
        if x < 2 or x > 141 or y < 2 or y > 93: return False
        if x <= 8 or x >= 135 or y <= 8 or y >= 87: return True       # Belgrano, Marconi, Berutti, Alsina
        if abs(x - mitre_c(y)) <= 6.5: return True                     # Av. Mitre
        if 50 <= x <= 56: return True                                  # San Martin
        if 28 <= x <= 33 and x < 50: return True                       # Gral. Lavalle (vertical)
        for (a, b) in ((24, 28), (40, 45), (64, 69)):
            if a <= y <= b: return True
        return False
    PLAZA = (57, 47, 94, 86)
    def es_plaza(x, y):
        return PLAZA[0] <= x <= PLAZA[2] and PLAZA[1] <= y <= PLAZA[3] and abs(x - mitre_c(y)) > 6.5 + 1

    # calles / plaza
    for y in range(2, 94):
        for x in range(2, 142):
            if es_plaza(x, y): m.g[y][x] = 'p'
            elif es_calle(x, y): m.g[y][x] = '.'
            else: m.g[y][x] = 'B_'   # edificio sin asignar
    # edificios: lotes con distintas fachadas
    altos = ['T', 'U', 'V', 'Y', 'H', 'T', 'U', 'Y']
    for y in range(2, 94):
        for x in range(2, 142):
            if m.g[y][x] == 'B_':
                lote = (x // 9) * 31 + (y // 7) * 17
                m.g[y][x] = altos[(lote * 7 + lote // 5) % len(altos)]
    # veredas: calle junto a un edificio
    for y in range(2, 94):
        for x in range(2, 142):
            if m.g[y][x] == '.':
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    c = m.get(x + dx, y + dy)
                    if c not in ('.', ':', 'p', 'N'):
                        m.g[y][x] = ':'; break
    # veredas en el borde de la plaza (para que el piso cambie)
    # ---------------------------------------------------------------
    # LUGARES
    # ---------------------------------------------------------------
    lugares = []   # dicts
    def lugar(nombre, ch, x0, y0, x1, y1, puerta, piso, descr, zona_id):
        # anillo exterior
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                borde = x in (x0, x1) or y in (y0, y1)
                m.g[y][x] = ch if borde else piso
        px, py = puerta
        m.g[py][px] = 'D'
        # salida a la calle (la puerta da a una vereda)
        lg = dict(nombre=nombre, ch=ch, x0=x0, y0=y0, x1=x1, y1=y1, puerta=puerta, piso=piso, zona=zona_id, descr=descr)
        lugares.append(lg)
        return lg
    def lado_puerta(lg):
        px, py = lg['puerta']
        if py == lg['y0']: return 'N'
        if py == lg['y1']: return 'S'
        if px == lg['x0']: return 'W'
        return 'E'
    def fuera(lg):
        px, py = lg['puerta']; s = lado_puerta(lg)
        return {'N': (px + .5, py - .5), 'S': (px + .5, py + 1.5), 'W': (px - .5, py + .5), 'E': (px + 1.5, py + .5)}[s]
    def dentro(lg):
        px, py = lg['puerta']; s = lado_puerta(lg)
        return {'N': (px + .5, py + 1.5), 'S': (px + .5, py - .5), 'W': (px + 1.5, py + .5), 'E': (px - .5, py + .5)}[s]

    L = {}
    L['mcd']   = lugar("McDonald's", 'm', 60, 9, 73, 18, (66, 9), ';', "Local de comidas rápidas. La cocina quedó intacta.", 'a')
    L['bk']    = lugar("Burger King", 'b', 76, 9, 88, 17, (82, 9), ';', "Otro local de hamburguesas, con la cocina abierta.", 'b')
    L['ciec']  = lugar("Instituto CIEC", 'i', 106, 9, 115, 17, (110, 9), ',', "Instituto de estudios. Aulas y oficinas.", 'c')
    L['cerr']  = lugar("Cerrajería Ferretería 25 de Mayo", 'c', 117, 9, 126, 17, (121, 9), ',', "Ferretería con herramientas.", 'd')
    L['casa_radio'] = lugar("Casa abandonada", 'H', 128, 9, 134, 16, (131, 9), ',', "La casa donde quedó una radio antigua.", 'e')
    L['escuela'] = lugar("Escuela Técnica Nº 7", 'e', 112, 47, 134, 62, (134, 58), ',', "Tu escuela. Aulas, taller y cocina.", 'f')
    L['edificio'] = lugar("Edificio (tu departamento)", 'a', 124, 70, 134, 76, (134, 73), ';', "Hall del edificio. El ascensor sube a tu departamento.", 'g')
    L['dental'] = lugar("Dental Total", 'y', 110, 79, 120, 86, (115, 86), ';', "Consultorio odontológico con botiquines.", 'h')
    L['racing'] = lugar("Sede Racing Club", 'r', 122, 79, 134, 86, (128, 86), ',', "La sede del club. Quedaron trofeos y equipamiento.", 'i')
    L['univ']  = lugar("Universidad Siglo 21", 'u', 34, 47, 49, 62, (49, 54), ',', "Sede de la universidad. Aulas y oficinas.", 'j')
    L['kiosco'] = lugar("Kiosco", 'K', 43, 71, 49, 78, (49, 75), ',', "Un kiosco con una luz encendida. Alguien se refugió adentro.", 'k')
    L['super'] = lugar("Supermercado", 'g', 14, 71, 27, 84, (27, 78), ';', "Supermercado de barrio. Góndolas revueltas.", 'l')
    L['farmacia'] = lugar("Farmacia", 'f', 9, 48, 20, 58, (9, 53), ';', "Farmacia con el mostrador intacto.", 'm')
    L['casa2'] = lugar("Casa de familia", 'H', 9, 9, 20, 19, (9, 14), ',', "Una casa cerrada hace días.", 'n')

    # tabiques interiores: (lugar, [(x0,y0,x1,y1), hueco)
    def tabique(lg, x0, y0, x1, y1, huecos=()):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                m.g[y][x] = lg['ch'] if (x, y) not in huecos else lg['piso']
    tabique(L['escuela'], 120, 48, 120, 61, huecos=[(120, 53), (120, 54), (120, 58)])
    tabique(L['escuela'], 121, 54, 133, 54, huecos=[(126, 54), (127, 54)])
    tabique(L['super'], 14, 77, 21, 77, huecos=[(17, 77), (18, 77)])
    tabique(L['racing'], 125, 80, 125, 85, huecos=[(125, 82), (125, 83)])
    tabique(L['univ'], 34, 54, 41, 54, huecos=[(37, 54), (38, 54)])
    # la puerta debe seguir siendo puerta
    for lg in lugares: m.g[lg['puerta'][1]][lg['puerta'][0]] = 'D'

    # nombres de zonas
    zonas = {}   # codigo -> (id, nombre, descripcion)
    def zona(cod, nombre, descr=''):
        zonas[cod] = (len(zonas) + 1, nombre, descr)
    zona('0', 'Avellaneda', 'Calles de Avellaneda cubiertas de nieve.')
    zona('1', 'Plaza Alsina', 'La plaza, bajo metros de nieve.')
    zona('2', 'Av. Mitre', 'Avenida ancha, con autos abandonados.')
    zona('3', 'Av. Manuel Belgrano', 'Avenida del oeste.')
    zona('4', 'Alsina', 'Calle Alsina, junto a la plaza.')
    zona('5', 'Berutti', 'Calle Berutti, al norte.')
    zona('6', 'Ing. Marconi', 'La calle de la Técnica 7.')
    zona('7', 'San Martín', 'Calle San Martín.')
    zona('8', 'Departamento', 'Tu departamento, en el piso de arriba.')
    for lg in lugares: zona(lg['zona'], lg['nombre'], lg['descr'])
    # pintar zonas: calles primero
    for y in range(2, 94):
        for x in range(2, 142):
            c = m.g[y][x]
            if es_plaza(x, y): m.zona[y][x] = '1'
            elif c in '.:':
                if x <= 8: m.zona[y][x] = '3'
                elif x >= 135: m.zona[y][x] = '6'
                elif y <= 8: m.zona[y][x] = '5'
                elif y >= 87: m.zona[y][x] = '4'
                elif abs(x - mitre_c(y)) <= 6.5: m.zona[y][x] = '2'
                elif 50 <= x <= 56: m.zona[y][x] = '7'
    for lg in lugares: m.zrect(lg['x0'], lg['y0'], lg['x1'], lg['y1'], lg['zona'])

    # ---------------------------------------------------------------
    # DEPARTAMENTO (anexo fuera del mapa)
    # ---------------------------------------------------------------
    ax, ay = ANX, ANY
    rows = [
        "######################",
        "#,,,,,,,,,,,,,,,,,,,,#",
        "#,,,,,,,,,,,,,,,,,,,,#",
        "#,,,,,,,,,,,,,,,,,,,,#",
        "#,,,,,,,,,,,,,,,,,,,,#",
        "#,,,,,,,,,,,,,,,,,,,,#",
        "####,,######,,########",
        "#,,,,,,,,,#,,,,,,,,,,#",
        "#,,,,,,,,,#,,,,,,,,,,#",
        "#,,,,,,,,,#,,,,,,,,,,#",
        "#,,,,,,,,,#,,,,,,,,,,#",
        "#,,,,,,,,,#,,,,,,,,,,#",
        "######################",
    ]
    for j, r in enumerate(rows):
        for i, c in enumerate(r):
            m.g[ay + j][ax + i] = 'H' if c == '#' else c
    m.zrect(ax, ay, ax + 21, ay + 12, '8')

    # decoración y objetos del departamento (ids 100..)
    m.cont('cuadro_familiar', ax + 10.5, ay + 1.12, 0, 'Cuadro', None, None, 0, 'Cuadro de la familia', 100,
           "Celeste, Mateo y Patricio. Hace días que no sé nada de ellos. Tengo que salir.")
    m.deco('mesa', ax + 8.2, ay + 3.2, 0, True, 0.45, 100)
    m.deco('silla', ax + 7.2, ay + 3.6, 90, False, 0.2, 100)
    m.deco('silla', ax + 9.2, ay + 3.6, -90, False, 0.2, 100)
    m.deco('sillon', ax + 14.5, ay + 4.3, 180, True, 0.5, 100)
    m.deco('lampara', ax + 2.6, ay + 1.6, 0, True, 0.25, 100)
    m.deco('lampara', ax + 18.6, ay + 4.6, 0, True, 0.25, 100)
    m.cont('heladera', ax + 17.2, ay + 1.4, 0, 'Heladera', 'heladera_vacia', None, 0, 'Heladera', 100)
    m.cont('cajon', ax + 4.5, ay + 1.25, 0, 'Cajon', 'sala', '1x1,8x1', 0, 'Cajón de la sala', 100)
    m.cont('ascensor', ax + 20.82, ay + 3.0, 90, 'Ascensor', None, None, 0, 'Ascensor', 100)
    # dormitorio
    m.deco('cama', ax + 3.6, ay + 10.2, 0, True, 0.5, 100)
    m.cont('cajon', ax + 5.9, ay + 10.55, 0, 'Cajon', None, '11x1,10x5', 0, 'Cajón de la mesa de luz', 100)
    m.deco('lampara', ax + 8.4, ay + 7.6, 0, True, 0.25, 100)
    # taller
    m.deco('mesa_trabajo', ax + 17.2, ay + 10.3, 180, True, 0.45, 100)
    m.cont('cajon', ax + 12.2, ay + 7.4, 0, 'Cajon', None, '5x1,7x1', 0, 'Cajón del taller', 100)
    m.cont('cajon', ax + 19.4, ay + 7.4, 0, 'Cajon', None, '6x1', 0, 'Cajón de herramientas', 100)
    mesa_id = [e for e in m.ents if 'mesa_trabajo' in e][0]
    # la mesa de trabajo es interactiva: se declara aparte
    m.ents = [e for e in m.ents if 'mesa_trabajo' not in e]
    i = m.nid(100)
    m.ents.append('C(%d, "mesa_trabajo", %s, %s, 180f, TipoEntidad.MesaTrabajo, null, null, 0, "Mesa de trabajo", null)' % (i, f(ax + 17.2), f(ay + 10.3)))
    m.luz(ax + 2.6, ay + 1.6, 0.75, 1.0, 0.78, 0.5, 7.0)
    m.luz(ax + 15.0, ay + 3.0, 0.7, 0.9, 0.72, 0.5, 6.0)
    m.luz(ax + 16.5, ay + 9.5, 0.65, 0.8, 0.66, 0.45, 5.0)
    m.luz(ax + 4.5, ay + 9.0, 0.7, 0.35, 0.4, 0.5, 4.5)
    m.luz(ax + 12.0, ay + 4.0, 0.7, 0.25, 0.3, 0.38, 6.0)
    INICIO = (ax + 10.5, ay + 4.3, -90.0)
    CAMA = (ax + 4.7, ay + 8.4, 0.0)

    # ---------------------------------------------------------------
    # AMBIENTACIÓN DE LOS LUGARES
    # ---------------------------------------------------------------
    def interior(lg):
        return lg['x0'] + 1, lg['y0'] + 1, lg['x1'] - 1, lg['y1'] - 1
    def libre_interior(x, y):
        return m.get(int(x), int(y)) in ',;'
    def poner_contra_fondo(lg, sprite, n, tabla, fijo=None, tipo='Cajon', nombre=None, completa=0, offset=0.0, desde=0.2):
        # coloca n contenedores en la pared del fondo (la opuesta a la puerta)
        ix0, iy0, ix1, iy1 = interior(lg)
        s = lado_puerta(lg)
        ids = []
        for k in range(n):
            t = (k + 1) / (n + 1)
            if s == 'N':   x, y, a = ix0 + (ix1 - ix0 + 1) * t + offset, iy1 + 0.62, 180
            elif s == 'S': x, y, a = ix0 + (ix1 - ix0 + 1) * t + offset, iy0 + 0.38, 0
            elif s == 'W': x, y, a = ix1 + 0.62, iy0 + (iy1 - iy0 + 1) * t + offset, -90 if False else 90
            else:          x, y, a = ix0 + 0.38, iy0 + (iy1 - iy0 + 1) * t + offset, -90
            if s in 'WE': a = 90 if s == 'W' else -90
            ids.append(m.cont(sprite, x, y, a, tipo, tabla, fijo if k == 0 else None, completa, nombre, 200))
        return ids
    def poner_lateral(lg, sprite, n, tabla, fijo=None, tipo='Heladera', nombre=None, completa=0, pared='izq'):
        ix0, iy0, ix1, iy1 = interior(lg)
        s = lado_puerta(lg)
        for k in range(n):
            t = 0.25 + 0.5 * (k + 1) / (n + 1)
            # pared izquierda/derecha respecto de quien entra
            if s in 'NS':
                x = ix0 + 0.4 if pared == 'izq' else ix1 + 0.6
                a = -90 if pared == 'izq' else 90
                y = iy0 + (iy1 - iy0 + 1) * t
            else:
                y = iy0 + 0.4 if pared == 'izq' else iy1 + 0.6
                a = 0 if pared == 'izq' else 180
                x = ix0 + (ix1 - ix0 + 1) * t
            m.cont(sprite, x, y, a, tipo, tabla, fijo if k == 0 else None, completa, nombre, 200)
    def mesa_con_sillas(cx, cy):
        m.deco('mesa', cx, cy, 0, True, 0.45, 300)
        m.deco('silla', cx - 0.55, cy, 90, False, 0.2, 300)
        m.deco('silla', cx + 0.55, cy, -90, False, 0.2, 300)

    # McDonald's: cocina al fondo + mesas
    lg = L['mcd']
    poner_contra_fondo(lg, 'heladera', 2, 'heladera', None, 'Heladera', 'Heladera de la cocina', 2)
    poner_lateral(lg, 'cajon', 1, 'cajon_cocina', '12x1', 'Cajon', 'Cajón de la cocina', 0, 'der')
    for (x, y) in ((63, 12), (67, 12), (71, 12), (63, 15), (71, 15)): mesa_con_sillas(x + .5, y + .5)
    m.luz(66.5, 14.5, 0.7, 1.0, 0.82, 0.5, 8.0)
    # Burger King
    lg = L['bk']
    poner_contra_fondo(lg, 'heladera', 2, 'heladera', None, 'Heladera', 'Heladera de la cocina', 2)
    poner_lateral(lg, 'cajon', 1, 'cajon_cocina', None, 'Cajon', 'Cajón de la cocina', 0, 'izq')
    for (x, y) in ((79, 12), (84, 12), (79, 15), (84, 15)): mesa_con_sillas(x + .5, y + .5)
    m.luz(82.5, 13.0, 0.7, 1.0, 0.7, 0.4, 8.0)
    # Instituto CIEC
    lg = L['ciec']
    poner_contra_fondo(lg, 'cajon', 2, 'oficina', None, 'Cajon', 'Archivero')
    poner_lateral(lg, 'cajon', 1, 'oficina', None, 'Cajon', 'Archivero', 0, 'der')
    mesa_con_sillas(108.5, 12.5); mesa_con_sillas(112.5, 12.5)
    m.luz(110.5, 13.0, 0.7, 0.85, 0.85, 0.7, 7.0)
    # Cerrajería
    lg = L['cerr']
    poner_contra_fondo(lg, 'cajon', 3, 'ferreteria', '13x1', 'Cajon', 'Cajón de herramientas')
    m.deco('estanteria', 119.5, 11.6, -90, True, 0.4, 300)
    m.deco('estanteria', 124.5, 11.6, 90, True, 0.4, 300)
    m.luz(121.5, 13.0, 0.7, 0.9, 0.8, 0.55, 7.0)
    # Casa abandonada (radio)
    lg = L['casa_radio']
    m.cont('radio', 129.0, 15.35, 180, 'Radio', None, None, 0, 'Radio antigua', 200)
    poner_lateral(lg, 'cajon', 1, 'cajon', None, 'Cajon', 'Cajón', 0, 'der')
    m.deco('silla_rota', 131.5, 12.5, 20, False, 0.2, 300)
    m.luz(130.5, 13.0, 0.65, 0.9, 0.72, 0.5, 6.0)
    # Escuela
    lg = L['escuela']
    for (x, y) in ((123, 50), (126, 50), (129, 50), (131, 50)): m.deco('mesa', x + .5, y + .5, 0, True, 0.4, 300)
    m.deco('pizarron', 128.0, 48.9, 0, True, 0.4, 300)
    for (x, y) in ((123, 57), (126, 57), (129, 57), (131, 57)): m.deco('mesa', x + .5, y + .5, 0, True, 0.4, 300)
    m.deco('pizarron', 128.0, 61.1, 180, True, 0.4, 300)
    m.cont('cajon', 114.4, 49.5, -90, 'Cajon', 'oficina', None, 0, 'Archivero de preceptoría', 200)
    m.cont('cajon', 114.4, 51.5, -90, 'Cajon', 'ferreteria', None, 0, 'Cajón del taller', 200)
    m.cont('heladera', 114.6, 60.5, -90, 'Heladera', 'heladera', None, 0, 'Heladera de la cocina', 200)
    m.cont('cajon', 114.4, 57.5, -90, 'Cajon', 'cajon', None, 0, 'Cajón de la preceptoría', 200)
    m.cont('cajon', 132.4, 58.0, 90, 'Cajon', 'cajon', None, 0, 'Cajón del aula', 200)
    m.deco('estanteria', 117.0, 49.0, 0, True, 0.4, 300)
    m.luz(116.0, 55.0, 0.7, 0.85, 0.85, 0.75, 8.0)
    m.luz(127.0, 52.0, 0.7, 0.8, 0.82, 0.78, 8.0)
    m.luz(127.0, 58.0, 0.7, 0.8, 0.82, 0.78, 8.0)
    # Edificio (lobby)
    lg = L['edificio']
    m.cont('ascensor', 125.18, 73.5, -90, 'Ascensor', None, None, 0, 'Ascensor', 200)
    m.deco('mesa', 130.0, 71.8, 0, True, 0.4, 300)
    m.deco('planta', 133.2, 71.7, 0, True, 0.2, 300)
    m.luz(129.0, 73.5, 0.7, 0.9, 0.85, 0.65, 6.0)
    LOBBY = (127.0, 73.5, 0.0)
    # Dental Total
    lg = L['dental']
    poner_contra_fondo(lg, 'cajon', 3, 'botica', None, 'Cajon', 'Botiquín del consultorio')
    m.deco('silla', 112.5, 82.5, 0, False, 0.2, 300)
    m.deco('silla', 118.0, 82.5, 0, False, 0.2, 300)
    m.luz(115.5, 82.5, 0.7, 0.8, 0.9, 0.95, 7.0)
    # Racing
    lg = L['racing']
    poner_contra_fondo(lg, 'cajon', 2, 'cajon', '14x1', 'Cajon', 'Vestuario')
    poner_lateral(lg, 'heladera', 1, 'heladera', None, 'Heladera', 'Heladera del buffet', 0, 'der')
    m.deco('banco', 124.5, 83.5, 0, True, 0.4, 300)
    m.deco('banco', 131.0, 83.5, 0, True, 0.4, 300)
    m.luz(125.0, 82.5, 0.7, 0.75, 0.85, 1.0, 7.0)
    m.luz(131.0, 82.5, 0.7, 0.75, 0.85, 1.0, 6.0)
    # Universidad
    lg = L['univ']
    poner_lateral(lg, 'cajon', 2, 'oficina', None, 'Cajon', 'Archivero', 0, 'izq')
    for (x, y) in ((37, 50), (40, 50), (44, 50), (37, 58), (41, 58), (45, 58)): m.deco('mesa', x + .5, y + .5, 0, True, 0.4, 300)
    m.cont('cajon', 36.5, 61.6, 180, 'Cajon', 'oficina', None, 0, 'Archivero', 200)
    m.luz(40.5, 51.0, 0.7, 0.8, 0.85, 0.9, 8.0)
    m.luz(40.5, 58.0, 0.7, 0.8, 0.85, 0.9, 8.0)
    # Kiosco (informante)
    lg = L['kiosco']
    m.cont('informante', 45.2, 74.4, 90, 'Personaje', None, None, 0, 'El Informante', 200)
    m.cont('heladera', 44.6, 72.5, 0, 'Heladera', 'heladera', None, 2, 'Heladera del kiosco', 200)
    m.cont('cajon', 44.6, 77.5, 180, 'Cajon', 'kiosco', None, 0, 'Cajón del kiosco', 200)
    m.deco('vela', 46.0, 75.8, 0, False, 0.1, 300)
    m.luz(46.0, 75.8, 0.36, 1.0, 0.7, 0.38, 4.0)
    # Supermercado
    lg = L['super']
    for k in range(3): m.deco('estanteria', 16.0 + k * 3.2, 73.5, 0, True, 0.4, 300)
    for k in range(3): m.deco('estanteria', 16.0 + k * 3.2, 75.4, 180, True, 0.4, 300)
    poner_contra_fondo(lg, 'heladera', 3, 'heladera', None, 'Heladera', 'Heladera del supermercado', 2)
    m.cont('cajon', 25.4, 73.0, -90 if False else 90, 'Cajon', 'alacena', None, 0, 'Cajón de la caja', 200)
    m.cont('cajon', 16.0, 79.4, 0, 'Cajon', 'alacena', None, 0, 'Cajón del depósito', 200)
    m.luz(20.5, 74.0, 0.7, 0.9, 0.85, 0.7, 8.0)
    m.luz(20.5, 81.0, 0.7, 0.9, 0.85, 0.7, 8.0)
    # Farmacia
    lg = L['farmacia']
    poner_contra_fondo(lg, 'cajon', 3, 'botica', None, 'Cajon', 'Estante de medicamentos')
    m.deco('mostrador', 12.5, 51.6, 90, True, 0.5, 300)
    m.luz(15.0, 53.0, 0.7, 0.8, 0.9, 0.95, 7.0)
    # Casa 2
    lg = L['casa2']
    poner_lateral(lg, 'cajon', 2, 'cajon', None, 'Cajon', 'Cajón', 0, 'der')
    poner_contra_fondo(lg, 'heladera', 1, 'heladera', None, 'Heladera', 'Heladera', 0)
    m.deco('cama', 17.5, 12.0, 0, True, 0.5, 300)
    m.luz(14.5, 14.0, 0.7, 0.9, 0.72, 0.5, 6.0)

    # ---------------------------------------------------------------
    # PLAZA Y CALLES: decoración exterior
    # ---------------------------------------------------------------
    pcx, pcy = 75.5, 66.5
    m.deco('monumento', pcx, pcy, 0, True, 0.6, 1000)
    for (dx, dy, a) in ((-3.0, 0, -90), (3.0, 0, 90), (0, -3.0, 0), (0, 3.0, 180)): m.deco('banco', pcx + dx, pcy + dy, a, True, 0.4, 1000)
    for (x, y) in ((60, 50), (92, 50), (60, 84), (92, 84), (66, 58), (84, 58), (66, 75), (84, 75), (75, 52), (75, 82), (60, 66), (91, 66)):
        m.deco('arbol', x + .5, y + .5, 0, True, 0.25, 1000)
    m.deco('cartel_plaza', 70.5, 85.4, 0, True, 0.15, 1000)
    m.deco('farola', 63.5, 62.5, 0, True, 0.15, 1000)
    m.deco('farola', 87.5, 70.5, 0, True, 0.15, 1000)
    m.deco('farola', 67.5, 79.5, 0, True, 0.15, 1000)
    m.deco('farola', 83.5, 54.5, 0, True, 0.15, 1000)
    # árboles y farolas sobre las veredas
    contador = 0
    for y in range(3, 93):
        for x in range(3, 141):
            if m.g[y][x] != ':': continue
            if (x * 7 + y * 13) % 17 == 0 and contador % 2 == 0:
                # solo si hay vereda libre de puertas
                ok = True
                for yy in range(y - 2, y + 3):
                    for xx in range(x - 2, x + 3):
                        if m.get(xx, yy) in 'dD': ok = False
                if ok: m.deco('arbol', x + .5, y + .5, 0, True, 0.25, 1000)
            if (x * 11 + y * 5) % 23 == 0:
                m.deco('farola', x + .5, y + .5, 0, True, 0.15, 1000)
            contador += 1
    # cartel de Av. Mitre y semáforos en esquinas
    m.deco('cartel_avenida', 96.5, 46.6, 0, True, 0.15, 1000)
    m.deco('cartel_avenida', 8.5, 30.5, 90, True, 0.15, 1000)
    for (x, y) in ((8.5, 8.5), (135.5, 8.5), (8.5, 87.5), (135.5, 87.5), (50.5, 45.6), (56.5, 39.6), (109.5, 45.6), (96.5, 69.6)):
        m.deco('semaforo', x, y, 0, True, 0.2, 1000)
    # autos en las calles
    autos = 0
    intentos = 0
    while autos < 46 and intentos < 4000:
        intentos += 1
        x = rnd.uniform(3, 140); y = rnd.uniform(3, 92)
        cx, cy = int(x), int(y)
        if m.g[cy][cx] != '.': continue
        # lejos del lobby y de las puertas
        if math.hypot(x - 135, y - 73) < 10: continue
        bien = True
        for yy in range(cy - 2, cy + 3):
            for xx in range(cx - 2, cx + 3):
                if m.get(xx, yy) not in '.:' : bien = False
        if not bien: continue
        # que no queden pegados
        if any(abs(x - ex) < 4.5 and abs(y - ey) < 4.5 for (ex, ey) in getattr(m, 'autos', [])): continue
        horizontal = (y <= 8.5 or y >= 87 or 24 <= y <= 28.9 or 40 <= y <= 45.9 or 64 <= y <= 69.9)
        ang = rnd.uniform(-8, 8) if horizontal else rnd.uniform(80, 100)
        if not horizontal and abs(x - mitre_c(y)) <= 6.5: ang = 90 + (math.degrees(math.atan(0.15)) * -1) + rnd.uniform(-6, 6)
        m.deco('auto_volcado' if rnd.random() < 0.2 else 'auto_nevado', x, y, ang, True, 0.8, 1000)
        if not hasattr(m, 'autos'): m.autos = []
        m.autos.append((x, y)); autos += 1
    # NE: cartel y barricada del puente
    m.deco('cartel_puente_chico', 130.5, 5.0, 0, True, 0.15, 1000)
    m.deco('auto_volcado', 124.0, 4.6, 8, True, 0.8, 1000)
    m.deco('farola', 132.5, 3.3, 0, True, 0.15, 1000)

    # ---------------------------------------------------------------
    # BICHOS: puntos de aparición
    # ---------------------------------------------------------------
    cand = []
    for y in range(4, 92, 3):
        for x in range(4, 140, 3):
            if m.g[y][x] in '.:p':
                if math.hypot(x - 135, y - 73) < 38: continue
                if math.hypot(x - 131, y - 6) < 12: continue
                cand.append((x + .5, y + .5, False))
    rnd.shuffle(cand)
    sp = []
    for c in cand:
        if all(math.hypot(c[0] - o[0], c[1] - o[1]) > 14 for o in sp): sp.append(c)
        if len(sp) >= 22: break
    # interiores (algunos)
    for nombre, (x, y) in (('mcd', (68.5, 14.5)), ('super', (22.5, 80.5)), ('univ', (44.5, 58.5)), ('escuela', (125.5, 60.5)), ('racing', (132.0, 82.5)), ('cerr', (123.0, 14.0))):
        sp.append((x, y, True))
    m.spawns = sp

    # ---------------------------------------------------------------
    # LUCES GLOBALES de calles (ninguna) y POSTALES
    # ---------------------------------------------------------------
    m.postales = [
        ('Departamento - living', INICIO[0], INICIO[1], 200),
        ('Departamento - taller', ax + 15.0, ay + 9.0, 145),
        ('Hall del edificio', LOBBY[0], LOBBY[1], 0),
        ('Plaza Alsina', 75.5, 56.0, 90),
        ('Av. Mitre', 99.0, 50.0, 80),
        ('Escuela Técnica 7', 138.0, 58.0, 180),
        ('McDonald\'s', 66.5, 6.0, 90),
        ('Burger King', 82.5, 6.0, 90),
        ('Sede Racing Club', 128.5, 90.0, -90),
        ('Ing. Marconi', 138.0, 73.0, -90),
        ('Entrada al puente', 131.0, 6.0, -90),
    ]
    return dict(m=m, lugares=lugares, zonas=zonas, inicio=INICIO, cama=CAMA, lobby=LOBBY, L=L,
                fuera=fuera, dentro=dentro, W=W, HP=HP, mitre_c=mitre_c)

# =====================================================================
# MAPA 2: PUENTE + AV. 9 DE JULIO
# =====================================================================
def puente():
    W, H = 56, 113
    m = Mapa(W, H)
    rnd = random.Random(99)
    for y in range(H):
        for x in range(W): m.g[y][x] = 'N'
    # Av. 9 de Julio (norte): filas 2..64
    for y in range(2, 65):
        for x in range(4, 52): m.g[y][x] = '.'
    # edificios a los costados
    altos = ['T', 'U', 'V', 'Y']
    for y in range(2, 65):
        for x in list(range(0, 4)) + list(range(52, 56)):
            m.g[y][x] = altos[((y // 6) + (x // 4)) % 4]
    # veredas
    for y in range(2, 65):
        for x in range(4, 52):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = m.get(x + dx, y + dy)
                if c in 'TUVYH': m.g[y][x] = ':'; break
    # canteros (islas verdes): son plaza 'p'
    for y in range(8, 58):
        for x in range(14, 19): m.g[y][x] = 'p'
        for x in range(36, 41): m.g[y][x] = 'p'
    for y in range(24, 37):
        for x in range(21, 34):
            if (x - 27) ** 2 + (y - 30) ** 2 <= 36: m.g[y][x] = 'p'
    # río y puente
    for y in range(65, 100):
        for x in range(0, 56): m.g[y][x] = '~'
        for x in range(22, 34): m.g[y][x] = '='
        m.g[y][22] = 'B'; m.g[y][33] = 'B'
    for x in range(4, 52): m.g[65][x] = ':' if (x < 22 or x > 33) else '='   # costanera
    for y in range(65, 66):
        for x in range(0, 4): m.g[y][x] = 'T'
        for x in range(52, 56): m.g[y][x] = 'T'
    for x in range(0, 56):
        if m.g[65][x] == '~': m.g[65][x] = 'T'
    # entrada del lado de Avellaneda
    for y in range(100, 111):
        for x in range(4, 52): m.g[y][x] = '.'
        for x in list(range(0, 4)) + list(range(52, 56)): m.g[y][x] = altos[(y // 5 + x) % 4]
    for x in range(4, 52):
        if x < 22 or x > 33:
            pass
    for y in range(100, 111):
        for x in range(4, 52):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = m.get(x + dx, y + dy)
                if c in 'TUVYH~': m.g[y][x] = ':'; break
    # pilares del portico
    m.g[100][21] = 'C'; m.g[100][34] = 'C'
    for x in range(4, 21): m.g[100][x] = 'T'
    for x in range(35, 52): m.g[100][x] = 'T'
    # (fila 100 es el cierre; 22..33 pasan)
    for x in range(22, 34): m.g[100][x] = '='
    # zonas
    zonas = {}
    def zona(cod, nombre, descr=''): zonas[cod] = (len(zonas) + 100, nombre, descr)
    zona('0', 'Av. 9 de Julio', 'La avenida más ancha de Buenos Aires, en guerra.')
    zona('1', 'Puente Pueyrredón', 'Puente cubierto de nieve sobre el Riachuelo.')
    zona('2', 'Obelisco', 'El Obelisco, rodeado de bolsas de arena.')
    zona('3', 'Acceso al puente', 'Avellaneda queda atrás.')
    m.zrect(0, 0, 55, 112, '0')
    m.zrect(0, 65, 55, 99, '1')
    m.zrect(0, 100, 55, 112, '3')
    m.zrect(17, 18, 38, 42, '2')
    # objetos
    for (x, y, a) in ((26.5, 98.0, 85), (29.5, 91.0, 95), (24.0, 85.0, 80), (30.5, 80.0, 100), (25.5, 73.0, 88)):
        m.deco('auto_nevado' if rnd.random() < .7 else 'auto_volcado', x, y, a, True, 0.8, 1000)
    for y in range(68, 98, 6):
        m.deco('farola', 22.6, y + .5, -90, True, 0.15, 1000)
        m.deco('farola', 33.4, y + 3.5, 90, True, 0.15, 1000)
    m.deco('cartel_puente', 28.0, 102.0, 0, True, 0.2, 1000)
    for (x, y, a) in ((14, 104, 5), (40, 106, -10), (30, 108, 90), (20, 101.6, 0)):
        m.deco('auto_volcado' if rnd.random() < .3 else 'auto_nevado', x + .5, y + .5, a, True, 0.8, 1000)
    # Avenida: autos
    autos = []
    n = 0
    while n < 20:
        x = rnd.uniform(5, 50); y = rnd.uniform(6, 62)
        cx, cy = int(x), int(y)
        if m.g[cy][cx] != '.': continue
        if abs(x - 27.5) < 8 and abs(y - 30.5) < 10: continue
        if any(abs(x - ex) < 4 and abs(y - ey) < 4 for (ex, ey) in autos): continue
        autos.append((x, y)); n += 1
        m.deco('auto_volcado' if rnd.random() < .25 else 'auto_nevado', x, y, rnd.uniform(78, 102), True, 0.8, 1100)
    for y in range(8, 60, 9):
        m.deco('farola', 12.5, y + .5, -90, True, 0.15, 1000)
        m.deco('farola', 42.5, y + 4.5, 90, True, 0.15, 1000)
        for x in (16.5, 38.5): m.deco('arbol', x, y + 3.5, 0, True, 0.25, 1000)
    # Obelisco (lo dibuja el generador) y bolsas de arena
    OBX, OBY = 27.5, 30.5
    m.deco('monumento_base', OBX, OBY, 0, True, 1.2, 1000)
    # soldados muertos y vivos
    muertos = [(22.0, 27.0, 20), (31.5, 25.5, 110), (19.5, 34.0, 200), (34.0, 33.5, 300), (25.5, 22.0, 80), (30.0, 38.5, 160), (23.0, 40.5, 45)]
    for (x, y, a) in muertos: m.deco('soldado_muerto', x, y, a, False, 0.3, 1200)
    vivos = [(19.5, 44.5, 0), (24.0, 45.5, 0), (31.5, 45.5, 0), (36.0, 44.5, 0)]
    for (x, y, a) in vivos: m.cont('soldado', x, y, a, 'Decoracion', None, None, 0, 'Soldado', 1300)
    for (x, y, a) in ((18.0, 46.5, 0), (21.5, 46.5, 0), (26.5, 47.0, 0), (28.5, 47.0, 0), (33.0, 46.5, 0), (37.5, 46.5, 0)):
        m.deco('bolsas_arena', x, y, a, True, 0.6, 1300)
    sx, sy = 28.0, 42.4
    m.cont('soldado', sx, sy, 0, 'Personaje', None, None, 0, 'Sargento', 1310)
    m.cont('cajon', 25.6, 42.6, 0, 'Cajon', 'ejercito', '10x12', 0, 'Caja de munición', 1320)
    m.cont('cajon', 31.0, 42.6, 0, 'Cajon', 'ejercito', None, 0, 'Caja del ejército', 1320)
    m.deco('camion', 14.5, 41.0, 90, True, 1.4, 1330)
    m.deco('camion', 41.5, 39.0, -90, True, 1.4, 1330)
    m.deco('camion', 12.0, 20.0, 160, True, 1.4, 1330)
    # bengalas / luces
    for (x, y) in ((18, 28), (37, 30), (27, 20), (27, 45)): m.luz(x, y, 1.0, 1.0, 0.25, 0.2, 6.0)
    # bichos: puntos
    sp = []
    for (x, y) in ((24, 15), (32, 14), (20, 22), (36, 24), (27, 10), (22, 33), (34, 36), (28, 26), (16, 30), (40, 28), (26, 50), (30, 54), (20, 56), (36, 58), (24, 62), (32, 61), (28, 78), (27, 88)):
        sp.append((x + .5, y + .5, False))
    m.spawns = sp
    m.postales = [('Entrada al puente', 28.0, 106.0, -90), ('Puente Pueyrredón', 28.0, 85.0, -90), ('Av. 9 de Julio', 28.0, 62.0, -90), ('Obelisco', 27.5, 40.0, -90)]
    return dict(m=m, zonas=zonas, W=W, H=H, OB=(OBX, OBY), SARG=(sx, sy))

# =====================================================================
# VALIDACIÓN
# =====================================================================
def bfs(m, sx, sy):
    seen = set([(sx, sy)]); dq = deque([(sx, sy)])
    while dq:
        x, y = dq.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if (nx, ny) in seen: continue
            c = m.get(nx, ny)
            if caminable(c):
                seen.add((nx, ny)); dq.append((nx, ny))
    return seen

def vista(m, x0, y0, x1, y1):
    return '\n'.join(''.join(m.g[y][x0:x1]) for y in range(y0, y1))

def emitir():
    A = avellaneda(); B = puente()
    ma, mb = A['m'], B['m']
    # --- validar Avellaneda ---
    sx, sy = int(A['inicio'][0]), int(A['inicio'][1])
    # el departamento es una isla: se valida por separado
    sdep = bfs(ma, sx, sy)
    ax, ay = 100, 98
    for (nombre, x, y) in (('mesa trabajo', ax + 17, ay + 10), ('cama', ax + 4, ay + 10), ('ascensor', ax + 19, ay + 3)):
        assert (x, y) in sdep, 'departamento: ' + nombre + ' inalcanzable'
    sl = bfs(ma, int(A['lobby'][0]), int(A['lobby'][1]))
    for lg in A['lugares']:
        px, py = lg['puerta']
        fx, fy = A['fuera'](lg)
        assert (int(fx), int(fy)) in sl, 'calle inalcanzable frente a ' + lg['nombre']
        dx, dy = A['dentro'](lg)
        assert (int(dx), int(dy)) in sl, 'interior inalcanzable: ' + lg['nombre']
    # entidades interactivas alcanzables
    # (se revisa leyendo las coordenadas del texto generado)
    import re
    for e in ma.ents:
        mm = re.match(r'C\((\d+), "(\w+)", ([\d.]+)f, ([\d.]+)f', e)
        if mm:
            x, y = float(mm.group(3)), float(mm.group(4))
            ok = False
            for ddx in (-1, 0, 1):
                for ddy in (-1, 0, 1):
                    if (int(x) + ddx, int(y) + ddy) in sl or (int(x) + ddx, int(y) + ddy) in sdep: ok = True
            assert ok, 'contenedor aislado ' + e
    # puntos de bichos
    for (x, y, i) in ma.spawns:
        assert (int(x), int(y)) in sl, 'spawn inalcanzable %s,%s' % (x, y)
    # el puente (esquina NE)
    assert (131, 5) in sl
    # --- validar puente ---
    s2 = bfs(mb, 28, 108)
    assert (28, 3) in s2, 'la avenida no se alcanza desde la entrada'
    for (x, y, i) in mb.spawns:
        assert (int(x), int(y)) in s2, 'spawn puente inalcanzable %s,%s' % (x, y)
    sg = B['SARG']
    assert (int(sg[0]), int(sg[1] + 1)) in s2
    return A, B

def zonas_a_cs(m, zonas, prefijo_var):
    # una cadena por fila; cada carácter = índice (base 62) de la zona
    alfabeto = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
    cods = list(zonas.keys())
    idx = {c: alfabeto[i] for i, c in enumerate(cods)}
    filas = [''.join(idx[m.zona[y][x]] if m.zona[y][x] in idx else '0' for x in range(m.w)) for y in range(m.h)]
    return filas, cods

def cs_mapa(nombre, mm, zonas, extra):
    m = mm
    filas, cods = zonas_a_cs(m, zonas, nombre)
    out = []
    out.append('        public static DatosMapa %s()' % nombre)
    out.append('        {')
    out.append('            var d = new DatosMapa { ancho = %d, alto = %d };' % (m.w, m.h))
    out.append('            d.filas = new[]')
    out.append('            {')
    for y in range(m.h): out.append('                %s, // %d' % (q(''.join(m.g[y])), y))
    out.append('            };')
    out.append('            d.zonaFilas = new[]')
    out.append('            {')
    for y in range(m.h): out.append('                %s,' % q(filas[y]))
    out.append('            };')
    out.append('            d.zonaClaves = %s;' % q(''.join('0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'[i] for i in range(len(cods)))))
    out.append('            d.zonaIds = new[] { %s };' % ', '.join(str(zonas[c][0]) for c in cods))
    for c in cods:
        z = zonas[c]
        out.append('            d.escenarios.Add(new EscenarioDef { id = %d, nombre = %s, ubicacion = %s, descripcion = %s });' % (z[0], q(z[1]), q(extra['ubicacion']), q(z[2])))
    for e in m.ents: out.append('            d.entidades.Add(%s);' % e)
    for l in m.luces: out.append('            d.luces.Add(%s);' % l)
    for (x, y, i) in m.spawns: out.append('            d.spawns.Add(new SpawnBicho { x = %s, y = %s, interior = %s });' % (f(x), f(y), 'true' if i else 'false'))
    for (nm, x, y, a) in m.postales: out.append('            d.postales.Add(new Postal { nombre = %s, x = %s, y = %s, angulo = %s });' % (q(nm), f(x), f(y), f(a)))
    for l in m.lugares: out.append(l)
    for k, v in extra['cs']: out.append('            ' + k)
    out.append('            return d;')
    out.append('        }')
    return '\n'.join(out)

def main():
    A, B = emitir()
    ma, mb = A['m'], B['m']
    # lugares (para el mapa y los objetivos)
    for lg in A['lugares']:
        fx, fy = A['fuera'](lg)
        ma.lugares.append('            d.lugares.Add(new LugarMapa { nombre = %s, x = %s, y = %s, x0 = %d, y0 = %d, x1 = %d, y1 = %d });' % (
            q(lg['nombre']), f(fx), f(fy), lg['x0'], lg['y0'], lg['x1'], lg['y1']))
    ma.lugares.append('            d.lugares.Add(new LugarMapa { nombre = "Plaza Alsina", x = 75.5f, y = 66.5f, x0 = 57, y0 = 47, x1 = 94, y1 = 86 });')
    mb.lugares.append('            d.lugares.Add(new LugarMapa { nombre = "Obelisco", x = 27.5f, y = 36f, x0 = 20, y0 = 24, x1 = 35, y1 = 37 });')
    mb.lugares.append('            d.lugares.Add(new LugarMapa { nombre = "Puente Pueyrredón", x = 28f, y = 82f, x0 = 22, y0 = 66, x1 = 33, y1 = 99 });')
    ex_a = dict(ubicacion='Avellaneda', cs=[
        ('d.id = 1; d.nombre = "Avellaneda";', 0),
        ('d.inicioX = %s; d.inicioY = %s; d.inicioAngulo = %s;' % (f(A['inicio'][0]), f(A['inicio'][1]), f(A['inicio'][2])), 0),
        ('d.camaX = %s; d.camaY = %s;' % (f(A['cama'][0]), f(A['cama'][1])), 0),
        ('d.hallX = %s; d.hallY = %s;' % (f(A['lobby'][0]), f(A['lobby'][1])), 0),
        ('d.llegadaX = 131.5f; d.llegadaY = 5.2f; d.llegadaRadio = 5.5f;', 0),
        ('d.obeliscoX = 128f; d.obeliscoY = -150f; d.hayObelisco = true;', 0),
        ('d.salidaBajaY = 97;', 0),
        ('d.bichosMin = 4; d.bichosMax = 10;', 0),
    ])
    ex_b = dict(ubicacion='Capital Federal', cs=[
        ('d.id = 2; d.nombre = "Puente Pueyrredón y Av. 9 de Julio";', 0),
        ('d.inicioX = 28f; d.inicioY = 108.5f; d.inicioAngulo = -90f;', 0),
        ('d.camaX = 28f; d.camaY = 108.5f; d.hallX = 28f; d.hallY = 108.5f;', 0),
        ('d.llegadaX = 28f; d.llegadaY = 62f; d.llegadaRadio = 5f;', 0),
        ('d.obeliscoX = %s; d.obeliscoY = %s; d.hayObelisco = true;' % (f(B['OB'][0]), f(B['OB'][1])), 0),
        ('d.salidaBajaY = 200;', 0),
        ('d.bichosMin = 12; d.bichosMax = 16;', 0),
        ('d.puenteX0 = 22; d.puenteX1 = 33; d.puenteY0 = 65; d.puenteY1 = 99; d.hayPuente = true;', 0),
    ])
    cab = '''using System.Collections.Generic;

namespace Eternauta.Beta
{
    // ARCHIVO GENERADO por tools/generar_mapas.py (no editar a mano: se pisa al regenerar).
    // Mapa 1: Avellaneda (Plaza Alsina, Av. Mitre, Av. Belgrano, Alsina, Berutti e Ing. Marconi)
    //         tomado de la captura satelital: la grilla está "enderezada" (Alsina al sur, Berutti al norte).
    // Mapa 2: Puente Pueyrredón y Av. 9 de Julio con el Obelisco (Capítulo 2).
    // Leyenda: . calle  : vereda  p plaza  , piso de madera  ; baldosas  D/d puerta  N muro de nieve (límite)
    //          T U V Y edificios altos  H casa  m b c i e a y r u g f K fachadas de los comercios
    //          ~ río  = asfalto del puente  B vereda del puente  C pilar
    public static class MapasGenerados
    {
        static EntidadDef D(int id, string sprite, float x, float y, float ang, bool solido, float radio)
        {
            return new EntidadDef { id = id, sprite = sprite, x = x, y = y, angulo = ang, tipo = TipoEntidad.Decoracion, solido = solido, radio = radio };
        }

        static EntidadDef C(int id, string sprite, float x, float y, float ang, TipoEntidad tipo, string tabla, string fijo, int completa, string nombre, string texto)
        {
            return new EntidadDef { id = id, sprite = sprite, x = x, y = y, angulo = ang, tipo = tipo, tabla = tabla, fijo = fijo, completaObjetivo = completa, nombre = nombre, texto = texto,
                solido = true, radio = RadioDe(sprite) };
        }

        static float RadioDe(string sprite)
        {
            switch (sprite)
            {
                case "heladera": return 0.42f;
                case "cajon": return 0.4f;
                case "ascensor": return 0.3f;
                case "cuadro_familiar": return 0.05f;
                case "soldado": return 0.3f;
                default: return 0.4f;
            }
        }

        static LuzDef L(float x, float y, float h, float r, float g, float b, float radio)
        {
            return new LuzDef { x = x, y = y, h = h, r = r, g = g, b = b, radio = radio };
        }

'''
    cuerpo = cs_mapa('Avellaneda', ma, A['zonas'], ex_a) + '\n\n' + cs_mapa('Puente', mb, B['zonas'], ex_b)
    # luces y entidades usan constructores L()/D()/C(): se agregan a listas de LuzDef
    cuerpo = cuerpo.replace('d.luces.Add(L(', 'd.luces.Add(L(')
    texto = cab + cuerpo + '\n    }\n}\n'
    # los 'cuadro' etc. deben ser tipos válidos
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
    with open(SALIDA, 'w', encoding='utf-8') as fh: fh.write(texto)
    print('Escrito', SALIDA, len(texto) // 1024, 'kB')
    # vista previa
    print(vista(ma, 0, 0, 144, 96))
    print()
    print(vista(mb, 0, 0, 56, 113))

if __name__ == '__main__':
    main()
