using FamilyTree.Data;
using FamilyTree.Services;

namespace FamilyTree.Tests;

public sealed class FamilyTreeServiceTests
{
  private static List<Relative> Family()
  {
    return SeedData.Relatives.ToList();
  }

  [Fact]
  public void SeedContainsExactlyOneRoot()
  {
    Assert.Single(Family(), person => person.IsRoot);
  }

  [Fact]
  public void KinshipIsCalculatedFromTheSelectedFocus()
  {
    List<Relative> family = Family();

    Assert.Equal("Pai", FamilyTreeService.Kinship(family, 1, 2));
    Assert.Equal("Mãe", FamilyTreeService.Kinship(family, 1, 3));
    Assert.Equal("Irmã", FamilyTreeService.Kinship(family, 1, 18));
    Assert.Equal("Filha", FamilyTreeService.Kinship(family, 1, 5));
  }

  [Fact]
  public void OneSidedSpouseLinkIsResolvedInBothDirections()
  {
    List<Relative> family = Family();
    Relative lucas = family.Single(person => person.Id == 1);
    Relative sofia = family.Single(person => person.Id == 4);

    Assert.Contains(sofia, FamilyTreeService.RegisteredSpousesOf(family, lucas));
    Assert.Contains(lucas, FamilyTreeService.RegisteredSpousesOf(family, sofia));
  }

  [Fact]
  public void BidirectionalTreeContainsAncestorsAndDescendants()
  {
    BidirectionalTree tree = FamilyTreeService.Bidirectional(Family(), 1);

    Assert.Equal(1, tree.Ancestors.Id);
    Assert.Equal(1, tree.Descendants.Id);
    Assert.Contains(tree.Descendants.Children, child => child.Id == 5);
    Assert.Contains(tree.Ancestors.Children, parent => parent.Id == 2);
  }

  [Fact]
  public void PublicSeedContainsNoIdentifiersOrPhotos()
  {
    Assert.All(Family(), person =>
    {
      Assert.Null(person.TaxId);
      Assert.Null(person.PhotoUrl);
      Assert.DoesNotContain("Johnny", person.Name, StringComparison.OrdinalIgnoreCase);
    });
  }
}
