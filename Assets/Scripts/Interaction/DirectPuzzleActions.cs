using System;
using System.Linq;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Interaction
{
    /// <summary>Direct manipulation commits through the existing transactional solver.
    /// Drag previews never consume inventory, moves or undo entries.</summary>
    public static class DirectPuzzleActions
    {
        public static bool Editable(EnergyBoxSession session) => session != null && !session.Running && !session.Solved &&
            (!session.Tutorial.Active || session.Tutorial.Started);

        public static bool Install(EnergyBoxSession session, int socket, string kind)
        {
            if (!Editable(session) || socket < 0 || socket >= session.Sockets.Length || !session.Tutorial.Allows(kind)) return false;
            if (session.Tutorial.Active && session.Selected != socket) return false;
            string focus = session.Tutorial.Target;
            if (focus != null && session.Sockets[socket].id != focus) return false;
            int choice = Array.FindIndex(session.Sockets[socket].choices, c => c.kind == kind);
            return choice >= 0 && session.Set(socket, choice + 1);
        }

        public static int RotateChoice(EnergyBoxSession session, int socket, int original, int steps)
        {
            if (socket < 0 || socket >= session.Sockets.Length || original <= 0) return original;
            var node = session.Sockets[socket];
            var options = Enumerable.Range(1, node.choices.Length).Where(i => node.choices[i - 1].kind == node.choices[original - 1].kind).ToArray();
            int at = Array.IndexOf(options, original);
            return options[(at + steps % options.Length + options.Length) % options.Length];
        }

        public static bool Rotate(EnergyBoxSession session, int socket, int choice)
        {
            if (!Editable(session) || !session.Tutorial.Allows("rotate") || socket < 0 || socket >= session.Sockets.Length) return false;
            string focus = session.Tutorial.Target;
            if (focus != null && session.Sockets[socket].id != focus) return false;
            return session.Set(socket, choice);
        }

        public static int NearestSocket(EnergyBoxSession session, Vector3 localPoint, float radius)
        {
            if (!Editable(session) || !Finite(localPoint)) return -1;
            int result = -1; float nearest = radius * radius;
            for (int i = 0; i < session.Sockets.Length; i++)
            {
                string focus = session.Tutorial.Target;
                if (focus != null && session.Sockets[i].id != focus) continue;
                float distance = (session.Sockets[i].Position - localPoint).sqrMagnitude;
                if (distance < nearest) { nearest = distance; result = i; }
            }
            return result;
        }

        public static bool Finite(Vector3 value) => Data.Point3.IsFinite(value.x) && Data.Point3.IsFinite(value.y) && Data.Point3.IsFinite(value.z);
    }
}
