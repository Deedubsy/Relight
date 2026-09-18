using NUnit.Framework;
using Relight.World;
using UnityEngine;
namespace Relight.Authoring.Tests
{
    public sealed class ResourceNodeArtTests
    {
        [Test] public void CatalogueHasEveryKindVariantAndDepletionStage()
        {
            var art=ResourceNodeArtSet.Load();Assert.That(art,Is.Not.Null);
            foreach(ResourceNodeKind kind in System.Enum.GetValues(typeof(ResourceNodeKind)))
            for(int v=0;v<3;v++)for(int s=0;s<3;s++)
            {
                var sprite=art.Find(kind,v,s);Assert.That(sprite,Is.Not.Null,$"{kind}/{v}/{s}");
                Assert.That(sprite.rect.width,Is.EqualTo(418));
                Assert.That(sprite.texture.filterMode,Is.EqualTo(FilterMode.Bilinear));
                Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
                Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width));
                Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
            }
        }
        [TestCase(300,0)][TestCase(201,0)][TestCase(200,1)][TestCase(101,1)][TestCase(100,2)][TestCase(1,2)][TestCase(0,-1)]
        public void RemainingResourceSelectsDepletionStage(double left,int expected)
        {Assert.That(ResourceNodeArtSet.Stage(left,300),Is.EqualTo(expected));}
        [Test] public void ExhaustedTileHasNoSprite()
        {var art=ResourceNodeArtSet.Load();foreach(ResourceNodeKind kind in System.Enum.GetValues(typeof(ResourceNodeKind)))Assert.That(art.Find(kind,0,ResourceNodeArtSet.Stage(0,300)),Is.Null);}
    }
}
