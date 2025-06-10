using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grafikeditor14.Core
{
    /// <summary>
    /// Basisschnittstelle für alle Editor-Aktionen,
    /// die rückgängig gemacht oder wiederholt werden können.
    /// </summary>
    public interface IEditorCommand
    {
        void Execute();   // Aktion ausführen
        void Undo();      // Aktion rückgängig machen
    }
}
