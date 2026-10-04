using System.ComponentModel.DataAnnotations;
using QuickTix.Contracts.DTOs.SaleDTOs;

namespace QuickTix.Tests.Contracts
{
    /// <summary>
    /// La validación del DTO debe coincidir con la del repositorio (recorta y luego comprueba
    /// longitud ≤ 200), para que ambas capas acepten y rechacen exactamente lo mismo.
    /// </summary>
    public class VoidSaleDtoValidationTests
    {
        private static bool IsValid(string reason, out List<ValidationResult> results)
        {
            results = new List<ValidationResult>();
            var dto = new VoidSaleDTO { Reason = reason };
            return Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        }

        [Fact]
        public void Reason_200Chars_PaddedWithSpaces_IsValid()
        {
            var reason = "  " + new string('a', 200) + "   ";

            Assert.True(IsValid(reason, out var results), string.Join("; ", results.Select(r => r.ErrorMessage)));
        }

        [Fact]
        public void Reason_201NonSpaceChars_IsInvalid()
        {
            Assert.False(IsValid(new string('a', 201), out var results));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("200"));
        }

        [Fact]
        public void Reason_BlankOrEmpty_IsInvalid()
        {
            Assert.False(IsValid("", out _));
            Assert.False(IsValid("    ", out _));
        }
    }
}
