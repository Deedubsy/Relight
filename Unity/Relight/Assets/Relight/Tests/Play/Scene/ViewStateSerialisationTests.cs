using System;
using System.Reflection;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-13 acceptance, test 3: "view state never serialised". Three independent checks, because the failure could
    /// come from three directions:
    ///   1. the sim assembly must not reference the presentation assembly at all (the asmdef forbids it; this
    ///      catches the case where someone changes the asmdef),
    ///   2. <see cref="ViewState"/> and everything it holds must not be visitable (<c>IVisitable</c>) and must not
    ///      be a <see cref="UnityEngine.Object"/> — Unity serialises those,
    ///   3. no MonoBehaviour or ScriptableObject in the presentation or UI assemblies may hold view state in a
    ///      serialised field, which is the realistic mistake: a <c>[SerializeField] ViewState view;</c> would put it
    ///      in the scene file and give it a second, stale lifetime.
    /// </summary>
    public sealed class ViewStateSerialisationTests
    {
        private static readonly Type[] ViewTypes =
        {
            typeof(ViewState), typeof(DebugView), typeof(HudInset), typeof(TransportView),
            typeof(InspectionView), typeof(NavigationView), typeof(ObjectiveView), typeof(ViewMode)
        };

        [Test]
        public void TheSimulationNeverReferencesThePresentation()
        {
            var sim = typeof(SimState).Assembly;
            foreach (var reference in sim.GetReferencedAssemblies())
                Assert.That(reference.Name, Is.Not.EqualTo("Relight.Presentation").And.Not.EqualTo("Relight.UI"),
                    "Relight.Sim must not reference the view layer.");

            // And no sim field is typed as a view type, whatever the assembly graph says.
            foreach (var type in sim.GetTypes())
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Static
                                             | BindingFlags.Public | BindingFlags.NonPublic))
                Assert.That(Array.IndexOf(ViewTypes, f.FieldType), Is.LessThan(0),
                    $"{type.FullName}.{f.Name} is view state.");
        }

        [Test]
        public void ViewStateIsNotVisitableAndNotAUnityObject()
        {
            foreach (var t in ViewTypes)
            {
                if (t.IsEnum) continue;
                Assert.That(typeof(IVisitable).IsAssignableFrom(t), Is.False,
                    $"{t.Name} is visitable, so a save would walk it.");
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(t), Is.False,
                    $"{t.Name} is a UnityEngine.Object, so Unity would serialise it.");
                Assert.That(t.GetCustomAttribute<SerializableAttribute>(), Is.Null,
                    $"{t.Name} is [Serializable], which is how it would end up in a scene file.");
            }
        }

        [Test]
        public void NoSerialisedFieldAnywhereHoldsViewState()
        {
            foreach (var assembly in new[] { typeof(SimHost).Assembly, typeof(Relight.UI.UiShell).Assembly })
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(type)) continue;
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var serialised = f.IsPublic
                        ? !f.IsNotSerialized && f.GetCustomAttribute<NonSerializedAttribute>() == null
                        : f.GetCustomAttribute<SerializeField>() != null;
                    if (!serialised) continue;
                    Assert.That(Array.IndexOf(ViewTypes, f.FieldType), Is.LessThan(0),
                        $"{type.FullName}.{f.Name} would serialise view state into an asset or scene file.");
                }
            }
        }
    }
}
