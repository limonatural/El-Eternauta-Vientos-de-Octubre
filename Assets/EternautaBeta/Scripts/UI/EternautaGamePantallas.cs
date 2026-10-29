using System.Collections.Generic;
using UnityEngine;

namespace Eternauta.Beta
{
    // Pantallas agregadas para la muestra anual: hotbar, mapa descubierto (M), muerte,
    // capítulo 2 desbloqueado, "continuará" y el corte a negro.
    public partial class EternautaGame
    {
        Texture2D iconoFlecha;
        readonly Dictionary<int, Texture2D> texMapas = new Dictionary<int, Texture2D>();

        // ---------------- HOTBAR ----------------
        void DibujarHotbar()
        {
            const float w = 150f, h = 92f, gap = 10f;
            float x0 = 48f, y0 = AH - 48f - h;
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(x0 + i * (w + gap), y0, w, h);
                bool sel = i == hotbarSel;
                int id = inventario.hotbar[i];
                var rec = id != 0 ? ContenidoJuego.Recurso(id) : null;
                Caja(r, ConAlfa(Negro, 0.72f));
                if (sel) { Caja(r, ConAlfa(BotonSel, 0.9f)); Marco(r, Blanco, 3f); }
                else Marco(r, ConAlfa(Blanco, 0.3f), 1.5f);
                Texto(new Rect(r.x + 10, r.y + 4, 30, 28), (i + 1).ToString(), Estilo(false, 24, true, sel ? Blanco : Gris));
                if (rec == null)
                {
                    Texto(new Rect(r.x + 8, r.y + 24, w - 16, 40), "—", Estilo(true, 28, false, ConAlfa(Gris, 0.6f), TextAnchor.MiddleCenter));
                    continue;
                }
                Texto(new Rect(r.x + 8, r.y + 24, w - 16, 40), rec.nombre, Estilo(true, 28, sel, Blanco, TextAnchor.MiddleCenter));
                string det = "";
                if (rec.arma == TipoArma.Pistola) det = jugador.cargador + " / " + inventario.Cantidad(ContenidoJuego.IdMunicion);
                else if (rec.id == ContenidoJuego.IdMapa) det = "M";
                else if (rec.arma == TipoArma.Ninguna) det = "×" + inventario.Cantidad(id);
                var colDet = rec.arma == TipoArma.Pistola && jugador.cargador == 0 ? Hex(0xE04A3A) : (sel ? Blanco : Gris);
                Texto(new Rect(r.x + 8, r.yMax - 34, w - 16, 30), det, Estilo(false, 26, true, colDet, TextAnchor.MiddleRight));
            }
        }

        // ---------------- CORTE A NEGRO ----------------
        void DibujarCorte()
        {
            float a = CorteAlpha;
            if (a <= 0.001f || Event.current.type != EventType.Repaint) return;
            Fondo(new Color(0, 0, 0, a));
            if (!string.IsNullOrEmpty(corteTexto) && a > 0.6f)
                Texto(new Rect(160, AH / 2 - 130, AW - 320, 260), corteTexto, Estilo(false, 44, false, ConAlfa(Blanco, Mathf.Clamp01((a - 0.6f) * 2.5f)), TextAnchor.MiddleCenter, true));
        }

        // ---------------- MUERTE ----------------
        void DibujarMuerte()
        {
            float a = Mathf.Clamp01(tEscenaMuerte / 1.2f);
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(new Color(0.05f, 0f, 0f, 0.88f * a));
                Texto(new Rect(0, 190, AW, 110), "HAS MUERTO", Estilo(true, 96, true, ConAlfa(Hex(0xC94A3E), a), TextAnchor.MiddleCenter));
                Texto(new Rect(AW / 2 - 700, 330, 1400, 130), causaMuerteMostrada, Estilo(false, 38, false, ConAlfa(Blanco, a), TextAnchor.UpperCenter, true));
                string pista = mundo.datos.id == 1
                    ? "Si despertás, vas a estar en tu cama: todo habrá sido un mal sueño."
                    : "Si despertás, vas a volver al comienzo del tramo.";
                Texto(new Rect(AW / 2 - 700, 480, 1400, 60), pista, Estilo(false, 28, false, ConAlfa(Gris, a), TextAnchor.UpperCenter, true));
            }
            if (tEscenaMuerte < 1.5f) return;
            var b0 = new Rect(AW / 2 - 300, 620, 600, 76);
            var b1 = new Rect(AW / 2 - 300, 720, 600, 76);
            if (Encima(b0)) selMuerte = 0;
            if (Encima(b1)) selMuerte = 1;
            if (Boton(b0, "DESPERTAR  (EN LA CAMA)", true, selMuerte == 0, 800, 34)) ActivarMuerte(0);
            if (Boton(b1, "MENÚ PRINCIPAL", true, selMuerte == 1, 801, 34)) ActivarMuerte(1);
        }

        // ---------------- CAPÍTULO 2 DESBLOQUEADO ----------------
        void DibujarAviso()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fondo(Color.black);
                Texto(new Rect(0, 150, AW, 70), "FIN DEL PRÓLOGO", Estilo(true, 48, true, Gris, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 240, AW, 130), "CAPÍTULO 2 DESBLOQUEADO", Estilo(true, 96, true, Blanco, TextAnchor.MiddleCenter));
                Texto(new Rect(0, 380, AW, 60), "La voz en la radio", Estilo(true, 44, false, Azul, TextAnchor.MiddleCenter));
                Texto(new Rect(AW / 2 - 700, 470, 1400, 110), "Pablo todavía no sabe nada de su familia. Del otro lado del Riachuelo, alguien transmite por radio.", Estilo(false, 32, false, Blanco, TextAnchor.UpperCenter, true));
                Texto(new Rect(0, AH - 110, AW, 44), "FIN DE LA BETA DEL PRÓLOGO  ·  Gracias por jugar", Estilo(false, 26, false, Gris, TextAnchor.MiddleCenter));
            }
            var b0 = new Rect(AW / 2 - 300, 640, 600, 76);
            var b1 = new Rect(AW / 2 - 300, 740, 600, 76);
            if (Encima(b0)) selAviso = 0;
            if (Encima(b1)) selAviso = 1;
            if (Boton(b0, "JUGAR CAPÍTULO 2", true, selAviso == 0, 810, 34)) { audioJuego.Menu(); SeguirCapitulo2(); }
            if (Boton(b1, "MENÚ PRINCIPAL", true, selAviso == 1, 811, 34)) { audioJuego.Menu(); VolverAlMenu(); }
        }

        // ---------------- CONTINUARÁ ----------------
        void DibujarContinuara()
        {
            if (Event.current.type != EventType.Repaint) return;
            float a = Mathf.Clamp01(tContinuara / 2f);
            Fondo(new Color(0, 0, 0, a));
            float t = Mathf.Clamp01((tContinuara - 1.5f) / 1.5f);
            Texto(new Rect(0, AH / 2 - 160, AW, 130), "CONTINUARÁ...", Estilo(true, 110, true, ConAlfa(Blanco, t), TextAnchor.MiddleCenter));
            float t2 = Mathf.Clamp01((tContinuara - 3.5f) / 1.5f);
            Texto(new Rect(AW / 2 - 700, AH / 2 - 10, 1400, 100), "Pablo todavía no encontró a su familia.\nPero sabe que están en alguna parte, y los va a encontrar.", Estilo(false, 34, false, ConAlfa(Gris, t2), TextAnchor.UpperCenter, true));
            if (tContinuara > 5f)
                Texto(new Rect(0, AH - 110, AW, 44), "[Enter] volver al menú principal", Estilo(false, 28, false, ConAlfa(Gris, Mathf.Clamp01(tContinuara - 5f)), TextAnchor.MiddleCenter));
        }

        // ---------------- MAPA DESCUBIERTO ----------------
        static Color ColorCelda(char c)
        {
            switch (c)
            {
                case '.': return new Color(0.46f, 0.52f, 0.58f);
                case ':': return new Color(0.60f, 0.64f, 0.68f);
                case 'p': return new Color(0.32f, 0.48f, 0.38f);
                case ',': case ';': return new Color(0.50f, 0.42f, 0.33f);
                case '=': return new Color(0.28f, 0.30f, 0.33f);
                case 'D': case 'd': return new Color(0.90f, 0.66f, 0.22f);
                case '#': case 'N': return new Color(0.92f, 0.95f, 1f);
                case '~': return new Color(0.13f, 0.27f, 0.45f);
                case 'B': return new Color(0.58f, 0.60f, 0.62f);
                case 'C': return new Color(0.2f, 0.2f, 0.22f);
                case 'T': case 'U': case 'V': case 'Y': case 'H': return new Color(0.15f, 0.17f, 0.20f);
                default: return new Color(0.30f, 0.34f, 0.42f);   // fachadas de lugares donde se puede entrar
            }
        }

        Texture2D TexturaMapa(DatosMapa d, int alto, Descubierto desc)
        {
            Texture2D t;
            if (!texMapas.TryGetValue(d.id, out t) || t == null)
            {
                t = new Texture2D(d.ancho, alto, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "mapa_" + d.id };
                texMapas[d.id] = t;
                mapaSucio = true;
            }
            if (mapaSucio)
            {
                var px = new Color32[d.ancho * alto];
                var oculto = new Color32(7, 9, 11, 255);
                for (int y = 0; y < alto; y++)
                {
                    string fila = d.filas[y];
                    for (int x = 0; x < d.ancho; x++)
                    {
                        int bx = x / BloqueMapa, by = y / BloqueMapa;
                        bool visto = bx < desc.bw && by < desc.bh && desc.v[by * desc.bw + bx];
                        px[(alto - 1 - y) * d.ancho + x] = visto ? (Color32)ColorCelda(x < fila.Length ? fila[x] : '#') : oculto;
                    }
                }
                t.SetPixels32(px);
                t.Apply(false);
                mapaSucio = false;
            }
            return t;
        }

        void DibujarMapa()
        {
            if (Event.current.type != EventType.Repaint) return;
            var d = mundo.datos;
            var desc = DescubiertoDe(d.id);
            int alto = d.salidaBajaY > 0 ? d.salidaBajaY : d.alto;
            var tex = TexturaMapa(d, alto, desc);
            bool tengoMapa = inventario.Cantidad(ContenidoJuego.IdMapa) > 0 || d.id != 1;

            Fondo(ConAlfa(Negro, 0.92f));
            Texto(new Rect(100, 36, 1700, 70), "MAPA — " + d.nombre.ToUpperInvariant(), Estilo(true, 52, true, Blanco));

            // área del dibujo: 1100 × 800, centrada
            float cajaW = 1100f, cajaH = 790f;
            float escala = Mathf.Min(cajaW / d.ancho, cajaH / alto);
            float mw = d.ancho * escala, mh = alto * escala;
            var rm = new Rect(100 + (cajaW - mw) / 2f, 120 + (cajaH - mh) / 2f, mw, mh);
            Caja(new Rect(rm.x - 6, rm.y - 6, rm.width + 12, rm.height + 12), Panel);
            var antes = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(S(rm), tex, ScaleMode.StretchToFill, false);
            GUI.color = antes;
            Marco(rm, ConAlfa(Blanco, 0.4f), 1.5f);

            // lugares
            foreach (var l in d.lugares)
            {
                int bx = Mathf.FloorToInt(l.x / BloqueMapa), by = Mathf.FloorToInt(l.y / BloqueMapa);
                bool visto = bx >= 0 && by >= 0 && bx < desc.bw && by < desc.bh && desc.v[by * desc.bw + bx];
                if (!visto && !tengoMapa) continue;
                if (l.y >= alto) continue;
                float px = rm.x + l.x / d.ancho * rm.width, py = rm.y + l.y / alto * rm.height;
                var st = Estilo(false, 20, true, visto ? Blanco : ConAlfa(Gris, 0.9f), TextAnchor.MiddleCenter);
                float w = Mathf.Min(300f, AnchoTexto(st, l.nombre) + 14f);
                var rl = new Rect(Mathf.Clamp(px - w / 2f, rm.x, rm.xMax - w), py - 13, w, 26);
                Caja(rl, ConAlfa(Negro, 0.62f));
                Texto(rl, l.nombre, st);
            }

            // marcas del objetivo
            var obj = progresion.Actual;
            if (obj != null)
                foreach (var m in obj.marcas)
                {
                    if (m.mapa != d.id || m.y >= alto) continue;
                    float px = rm.x + m.x / d.ancho * rm.width, py = rm.y + m.y / alto * rm.height;
                    float pul = 11f + 3f * Mathf.Sin(Time.unscaledTime * 4f);
                    Caja(new Rect(px - pul, py - pul, pul * 2, pul * 2), ConAlfa(Azul, 0.9f));
                    Marco(new Rect(px - pul, py - pul, pul * 2, pul * 2), Blanco, 2f);
                }

            // el jugador (si está en el departamento, se marca el edificio)
            float jx = jugador.x, jy = jugador.y;
            bool arriba = d.salidaBajaY > 0 && jy >= d.salidaBajaY;
            if (arriba) { jx = d.hallX; jy = d.hallY; }
            if (iconoFlecha == null)
                iconoFlecha = Icono(new[]
                {
                    "....X....",
                    "...XXX...",
                    "..XXXXX..",
                    ".XXXXXXX.",
                    "XXXXXXXXX",
                    "...XXX...",
                    "...XXX...",
                    "...XXX...",
                });
            {
                float px = rm.x + jx / d.ancho * rm.width, py = rm.y + jy / alto * rm.height;
                var c = S(new Rect(px - 15, py - 15, 30, 30));
                var matAntes = GUI.matrix;
                GUIUtility.RotateAroundPivot(jugador.angulo * Mathf.Rad2Deg + 90f, c.center);
                GUI.color = Hex(0xFFD24A);
                GUI.DrawTexture(c, iconoFlecha, ScaleMode.ScaleToFit, true);
                GUI.matrix = matAntes;
                GUI.color = antes;
            }

            // panel derecho
            var rp = new Rect(1260, 120, 560, 790);
            Caja(rp, Panel);
            Marco(rp, ConAlfa(Blanco, 0.25f), 1f);
            float yy = rp.y + 20;
            Texto(new Rect(rp.x + 24, yy, rp.width - 48, 40), "REFERENCIAS", Estilo(true, 34, true, Gris)); yy += 56;
            Caja(new Rect(rp.x + 28, yy + 10, 24, 24), Hex(0xFFD24A));
            Texto(new Rect(rp.x + 68, yy, rp.width - 92, 44), arriba ? "Vos (estás en tu departamento)" : "Vos", Estilo(false, 28, false, Blanco)); yy += 52;
            Caja(new Rect(rp.x + 28, yy + 10, 24, 24), Azul);
            Texto(new Rect(rp.x + 68, yy, rp.width - 92, 44), "Objetivo", Estilo(false, 28, false, Blanco)); yy += 52;
            Caja(new Rect(rp.x + 28, yy + 10, 24, 24), ColorCelda('#'));
            Texto(new Rect(rp.x + 68, yy, rp.width - 92, 44), "Muro de nieve (límite)", Estilo(false, 28, false, Blanco)); yy += 52;
            Caja(new Rect(rp.x + 28, yy + 10, 24, 24), ColorCelda('.'));
            Texto(new Rect(rp.x + 68, yy, rp.width - 92, 44), "Calle", Estilo(false, 28, false, Blanco)); yy += 52;
            Caja(new Rect(rp.x + 28, yy + 10, 24, 24), ColorCelda('x'));
            Texto(new Rect(rp.x + 68, yy, rp.width - 92, 44), "Lugar donde se puede entrar", Estilo(false, 28, false, Blanco)); yy += 68;
            if (obj != null)
            {
                Texto(new Rect(rp.x + 24, yy, rp.width - 48, 40), "OBJETIVO", Estilo(true, 34, true, Gris)); yy += 50;
                Texto(new Rect(rp.x + 24, yy, rp.width - 48, 40), (obj.titulo + ProgresoObjetivo()).ToUpperInvariant(), Estilo(false, 28, true, Blanco)); yy += 46;
                Texto(new Rect(rp.x + 24, yy, rp.width - 48, 130), obj.descripcion, Estilo(false, 26, false, Gris, TextAnchor.UpperLeft, true)); yy += 140;
            }
            if (!tengoMapa)
                Texto(new Rect(rp.x + 24, rp.yMax - 150, rp.width - 48, 110), "Solo ves lo que exploraste. Con el Mapa de Avellaneda ves los nombres de todos los lugares.", Estilo(false, 24, false, Gris, TextAnchor.UpperLeft, true));
            Texto(new Rect(rp.x + 24, rp.yMax - 56, rp.width - 48, 40), "[M] o [Esc] cerrar", Estilo(false, 26, false, Gris, TextAnchor.MiddleRight));
        }
    }
}
