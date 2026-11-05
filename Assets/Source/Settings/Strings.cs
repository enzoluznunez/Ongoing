using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class Strings
{
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        ["panel.tools"] = new[] { "Tools", "Herramientas" },
        ["panel.settings"] = new[] { "Settings", "Ajustes" },
        ["wrist.tools"] = new[] { "Tool Panel", "Panel de herramientas" },
        ["wrist.settings"] = new[] { "Settings", "Ajustes" },
        ["undo"] = new[] { "Undo", "Deshacer" },
        ["undo.all"] = new[] { "Undo All", "Deshacer todo" },

        ["tool.Move"] = new[] { "Move", "Mover" },
        ["tool.Rotate"] = new[] { "Rotate", "Rotar" },
        ["tool.Scale"] = new[] { "Scale", "Escalar" },
        ["tool.Profile"] = new[] { "Profile", "Perfil" },
        ["tool.Filter"] = new[] { "Filter", "Filtrar" },
        ["tool.Sort"] = new[] { "Sort", "Ordenar" },
        ["tool.Move.desc"] = new[] { "Moves the sheet anywhere in the room.", "Mueve la hoja a cualquier lugar de la sala." },
        ["tool.Rotate.desc"] = new[] { "Turns the sheet to face any direction.", "Gira la hoja hacia cualquier dirección." },
        ["tool.Scale.desc"] = new[] { "Makes the sheet larger or smaller.", "Agranda o achica la hoja." },
        ["tool.Profile.desc"] = new[] { "Raises rows or columns above the sheet.", "Eleva filas o columnas sobre la hoja." },
        ["tool.Sort.desc"] = new[] { "Reorders the rows and columns of the sheet.", "Reordena las filas y columnas de la hoja." },
        ["tool.Filter.desc"] = new[] { "Hides or shows rows and columns on the sheet.", "Oculta o muestra filas y columnas de la hoja." },

        ["assistant"] = new[] { "Assistant", "Asistente" },
        ["assistant.desc"] = new[] { "Carries out your spoken requests using the same tools.", "Hace lo que le pides en voz alta con las mismas herramientas." },
        ["assistant.connecting"] = new[] { "Connecting", "Conectando" },
        ["speed.instant"] = new[] { "Instant", "Instantáneo" },

        ["setting.Animations"] = new[] { "Animations", "Animaciones" },
        ["setting.SoundEffects"] = new[] { "Sound Effects", "Efectos de sonido" },
        ["setting.Handedness"] = new[] { "Handedness", "Mano dominante" },
        ["setting.TextSize"] = new[] { "Text Size", "Tamaño del texto" },
        ["setting.ColorMode"] = new[] { "Color Mode", "Modo de color" },
        ["setting.Language"] = new[] { "Language Mode", "Modo de idioma" },
        ["setting.ExplanationLevel"] = new[] { "Voice Assistant's Explanation Level", "Nivel de explicación del asistente de voz" },

        ["option.on"] = new[] { "On", "Activado" },
        ["option.off"] = new[] { "Off", "Desactivado" },
        ["option.Left"] = new[] { "Left", "Izquierda" },
        ["option.Right"] = new[] { "Right", "Derecha" },
        ["option.Small"] = new[] { "Small", "Pequeño" },
        ["option.Medium"] = new[] { "Medium", "Mediano" },
        ["option.Large"] = new[] { "Large", "Grande" },
        ["option.Standard"] = new[] { "Standard", "Estándar" },
        ["option.RedGreen"] = new[] { "Red-Green", "Rojo-verde" },
        ["option.BlueYellow"] = new[] { "Blue-Yellow", "Azul-amarillo" },
        ["option.Simple"] = new[] { "Simple", "Sencillo" },
        ["option.Expert"] = new[] { "Expert", "Experto" },
        ["option.English"] = new[] { "English", "English" },
        ["option.Spanish"] = new[] { "Español", "Español" },

        ["colorkey.desc"] = new[] { "Shows what each color stands for on the sheet.", "Muestra qué representa cada color en la hoja." },
        ["colorkey.other"] = new[] { "Everything else", "Todo lo demás" },
        ["colorkey.none"] = new[] { "This sheet has no colors to explain.", "Esta hoja no tiene colores que explicar." },

        ["loading"] = new[] { "Loading", "Cargando" },
        ["loading.listing"] = new[] { "Listing the sheets", "Enumerando las hojas" },
        ["loading.reading"] = new[] { "Reading sheets {0} of {1}", "Leyendo hojas {0} de {1}" },
        ["loading.sheets"] = new[] { "Building the sheets", "Construyendo las hojas" },
        ["loading.tools"] = new[] { "Building the tools", "Preparando las herramientas" },
        ["loading.graphics"] = new[] { "Preparing the graphics", "Preparando los gráficos" },

        ["notice.assistantOff"] = new[] { "Assistant Off", "Asistente apagado" },
        ["notice.micDenied"] = new[] { "Microphone access is denied, so the assistant cannot listen.", "No hay acceso al micrófono, así que el asistente no puede escuchar." },
        ["notice.disconnected"] = new[] { "The assistant could not stay connected and turned itself off.", "El asistente no pudo mantenerse conectado y se apagó." },
        ["notice.nearlyFull"] = new[] { "Assistant Nearly Full", "Asistente casi lleno" },
        ["notice.nearlyFull.body"] = new[] { "The assistant is close to the limit of what it can hold in mind. It will reset itself shortly and start over from a blank slate.", "El asistente está cerca del límite de lo que puede recordar. Pronto se reiniciará y empezará de cero." },
        ["notice.full"] = new[] { "Assistant Full", "Asistente lleno" },
        ["notice.full.open"] = new[] { "The assistant ran out of room to hold the conversation and stopped. Select the assistant again to start over.", "El asistente se quedó sin espacio para la conversación y se detuvo. Selecciona el asistente de nuevo para empezar otra vez." },
        ["notice.full.closed"] = new[] { "The assistant ran out of room to hold the conversation and stopped. To start over, open the Tool Panel and select the assistant.", "El asistente se quedó sin espacio para la conversación y se detuvo. Para empezar otra vez, abre el panel de herramientas y selecciona el asistente." },
        ["notice.stopped"] = new[] { "Assistant Stopped", "Asistente detenido" },
        ["notice.stopped.one"] = new[] { "One change was already made. Undo takes it back.", "Ya se hizo un cambio. Deshacer lo revierte." },
        ["notice.stopped.many"] = new[] { "{0} changes were already made. Undo takes them back in one step.", "Ya se hicieron {0} cambios. Deshacer los revierte en un solo paso." },
        ["notice.filter"] = new[] { "Filter", "Filtrar" },
        ["notice.noSheets"] = new[] { "No Sheets", "Sin hojas" },
        ["notice.noSheets.body"] = new[] { "There is no {0} in StreamingAssets to list the sheets from.", "No hay ningún {0} en StreamingAssets del que listar las hojas." },
        ["notice.sheetUnavailable"] = new[] { "Sheet Unavailable", "Hoja no disponible" },
        ["notice.sheetUnavailable.body"] = new[] { "{0} could not be read.", "No se pudo leer {0}." },
    };

    private static int Column => Settings.Language == AppLanguage.Spanish ? 1 : 0;

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out string[] row))
        {
            Debug.LogWarning($"[Strings] No text for '{key}'.");
            return key;
        }
        return row[Column];
    }

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    public static event Action Changed
    {
        add { Listen(); _changed += value; }
        remove { _changed -= value; }
    }
    private static Action _changed;

    private static readonly List<(TMP_Text label, string key)> _bound = new List<(TMP_Text, string)>();
    private static bool _listening;

    public static void Bind(TMP_Text label, string key)
    {
        if (label == null) return;
        Listen();
        for (int i = 0; i < _bound.Count; i++)
        {
            if (_bound[i].label != label) continue;
            _bound[i] = (label, key);
            label.text = Get(key);
            return;
        }
        _bound.Add((label, key));
        label.text = Get(key);
    }

    private static void Listen()
    {
        if (_listening) return;
        _listening = true;
        Settings.Changed += OnSettingChanged;
    }

    private static void OnSettingChanged(Setting which)
    {
        if (which != Setting.Language) return;
        for (int i = _bound.Count - 1; i >= 0; i--)
        {
            if (_bound[i].label == null) { _bound.RemoveAt(i); continue; }
            _bound[i].label.text = Get(_bound[i].key);
        }
        _changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _bound.Clear();
        _listening = false;
        _changed = null;
    }
}
