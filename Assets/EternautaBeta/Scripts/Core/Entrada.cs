using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Eternauta.Beta
{
    // Teclas que usa la beta. Se traducen al Input System nuevo (el que tiene
    // activado el proyecto) o al Input Manager viejo si el proyecto lo usa.
    public enum Tecla
    {
        W, A, S, D, Arriba, Abajo, Izquierda, Derecha,
        Shift, E, Espacio, Enter, Escape, Tab, I, Uno, Dos,
        F2, F3, F4, F5, F6, F7, F8, F9, F11, F12
    }

    public static class Entrada
    {
#if ENABLE_INPUT_SYSTEM
        static Key[][] cache;

        static Key[] Teclas(Tecla t)
        {
            if (cache == null)
            {
                var valores = (Tecla[])System.Enum.GetValues(typeof(Tecla));
                cache = new Key[valores.Length][];
                foreach (var v in valores) cache[(int)v] = Crear(v);
            }
            return cache[(int)t];
        }

        static Key[] Crear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return new[] { Key.W };
                case Tecla.A: return new[] { Key.A };
                case Tecla.S: return new[] { Key.S };
                case Tecla.D: return new[] { Key.D };
                case Tecla.Arriba: return new[] { Key.UpArrow };
                case Tecla.Abajo: return new[] { Key.DownArrow };
                case Tecla.Izquierda: return new[] { Key.LeftArrow };
                case Tecla.Derecha: return new[] { Key.RightArrow };
                case Tecla.Shift: return new[] { Key.LeftShift, Key.RightShift };
                case Tecla.E: return new[] { Key.E };
                case Tecla.Espacio: return new[] { Key.Space };
                case Tecla.Enter: return new[] { Key.Enter, Key.NumpadEnter };
                case Tecla.Escape: return new[] { Key.Escape, Key.P };
                case Tecla.Tab: return new[] { Key.Tab };
                case Tecla.I: return new[] { Key.I };
                case Tecla.Uno: return new[] { Key.Digit1, Key.Numpad1 };
                case Tecla.Dos: return new[] { Key.Digit2, Key.Numpad2 };
                case Tecla.F2: return new[] { Key.F2 };
                case Tecla.F3: return new[] { Key.F3 };
                case Tecla.F4: return new[] { Key.F4 };
                case Tecla.F5: return new[] { Key.F5 };
                case Tecla.F6: return new[] { Key.F6 };
                case Tecla.F7: return new[] { Key.F7 };
                case Tecla.F8: return new[] { Key.F8 };
                case Tecla.F9: return new[] { Key.F9 };
                case Tecla.F11: return new[] { Key.F11 };
                case Tecla.F12: return new[] { Key.F12 };
            }
            return new Key[0];
        }

        public static bool Mantenida(Tecla t)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in Teclas(t)) if (kb[k].isPressed) return true;
            return false;
        }

        public static bool Pulsada(Tecla t)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in Teclas(t)) if (kb[k].wasPressedThisFrame) return true;
            return false;
        }

        // Movimiento horizontal del mouse en píxeles de este frame.
        public static float MouseX()
        {
            var m = Mouse.current;
            return m == null ? 0f : m.delta.ReadValue().x;
        }

        public static bool MouseMovido()
        {
            var m = Mouse.current;
            return m != null && m.delta.ReadValue().sqrMagnitude > 0.01f;
        }
#else
        static KeyCode[][] cache;

        static KeyCode[] Teclas(Tecla t)
        {
            if (cache == null)
            {
                var valores = (Tecla[])System.Enum.GetValues(typeof(Tecla));
                cache = new KeyCode[valores.Length][];
                foreach (var v in valores) cache[(int)v] = Crear(v);
            }
            return cache[(int)t];
        }

        static KeyCode[] Crear(Tecla t)
        {
            switch (t)
            {
                case Tecla.W: return new[] { KeyCode.W };
                case Tecla.A: return new[] { KeyCode.A };
                case Tecla.S: return new[] { KeyCode.S };
                case Tecla.D: return new[] { KeyCode.D };
                case Tecla.Arriba: return new[] { KeyCode.UpArrow };
                case Tecla.Abajo: return new[] { KeyCode.DownArrow };
                case Tecla.Izquierda: return new[] { KeyCode.LeftArrow };
                case Tecla.Derecha: return new[] { KeyCode.RightArrow };
                case Tecla.Shift: return new[] { KeyCode.LeftShift, KeyCode.RightShift };
                case Tecla.E: return new[] { KeyCode.E };
                case Tecla.Espacio: return new[] { KeyCode.Space };
                case Tecla.Enter: return new[] { KeyCode.Return, KeyCode.KeypadEnter };
                case Tecla.Escape: return new[] { KeyCode.Escape, KeyCode.P };
                case Tecla.Tab: return new[] { KeyCode.Tab };
                case Tecla.I: return new[] { KeyCode.I };
                case Tecla.Uno: return new[] { KeyCode.Alpha1, KeyCode.Keypad1 };
                case Tecla.Dos: return new[] { KeyCode.Alpha2, KeyCode.Keypad2 };
                case Tecla.F2: return new[] { KeyCode.F2 };
                case Tecla.F3: return new[] { KeyCode.F3 };
                case Tecla.F4: return new[] { KeyCode.F4 };
                case Tecla.F5: return new[] { KeyCode.F5 };
                case Tecla.F6: return new[] { KeyCode.F6 };
                case Tecla.F7: return new[] { KeyCode.F7 };
                case Tecla.F8: return new[] { KeyCode.F8 };
                case Tecla.F9: return new[] { KeyCode.F9 };
                case Tecla.F11: return new[] { KeyCode.F11 };
                case Tecla.F12: return new[] { KeyCode.F12 };
            }
            return new KeyCode[0];
        }

        public static bool Mantenida(Tecla t)
        {
            foreach (var k in Teclas(t)) if (Input.GetKey(k)) return true;
            return false;
        }

        public static bool Pulsada(Tecla t)
        {
            foreach (var k in Teclas(t)) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        public static float MouseX()
        {
            return Input.GetAxisRaw("Mouse X") * 10f;
        }

        public static bool MouseMovido()
        {
            return Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.001f;
        }
#endif
    }
}
