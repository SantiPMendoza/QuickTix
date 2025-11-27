using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models;
using System.Net;

namespace QuickTix.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Cambiar a [Authorize(Roles = "admin")] cuando eso mi pana
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected ResponseApi _responseApi;

        public UserController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _responseApi = new ResponseApi();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersAsync()
        {
            try
            {
                List<UserDTO> userListDto = await _userRepository.GetUserDTOsAsync();
                _responseApi.StatusCode = HttpStatusCode.OK;
                _responseApi.IsSuccess = true;
                _responseApi.Result = userListDto;
                return Ok(_responseApi);
            }
            catch (System.Exception ex)
            {
                _responseApi.StatusCode = HttpStatusCode.InternalServerError;
                _responseApi.IsSuccess = false;
                _responseApi.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _responseApi);
            }
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUserAsync(string id)
        {
            try
            {
                var user = _userRepository.GetUserAsync(id);
                if (user == null)
                {
                    _responseApi.StatusCode = HttpStatusCode.NotFound;
                    _responseApi.IsSuccess = false;
                    _responseApi.ErrorMessages.Add("User not found.");
                    return NotFound(_responseApi);
                }
                var userDto = _mapper.Map<UserDTO>(user);
                _responseApi.StatusCode = HttpStatusCode.OK;
                _responseApi.IsSuccess = true;
                _responseApi.Result = userDto;
                return Ok(_responseApi);
            }
            catch (System.Exception ex)
            {
                _responseApi.StatusCode = HttpStatusCode.InternalServerError;
                _responseApi.IsSuccess = false;
                _responseApi.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _responseApi);
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDTO registrationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Incorrect Input", message = ModelState });

            if (!await _userRepository.IsUniqueUserAsync(registrationDto.UserName))
            {
                _responseApi.StatusCode = HttpStatusCode.BadRequest;
                _responseApi.IsSuccess = false;
                _responseApi.ErrorMessages.Add("Username already exists");
                return BadRequest(_responseApi);
            }

            var newUser = await _userRepository.RegisterAsync(registrationDto);
            if (newUser == null)
            {
                _responseApi.StatusCode = HttpStatusCode.BadRequest;
                _responseApi.IsSuccess = false;
                _responseApi.ErrorMessages.Add("Error registering the user");
                return BadRequest(_responseApi);
            }

            _responseApi.StatusCode = HttpStatusCode.OK;
            _responseApi.IsSuccess = true;
            _responseApi.Result = newUser;
            return Ok(_responseApi);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDTO loginDto)
        {
            var responseLogin = await _userRepository.LoginAsync(loginDto);

            if (responseLogin == null || responseLogin.User == null || string.IsNullOrEmpty(responseLogin.Token))
            {
                _responseApi.StatusCode = HttpStatusCode.BadRequest;
                _responseApi.IsSuccess = false;
                _responseApi.ErrorMessages.Add("Incorrect user or password");
                return BadRequest(_responseApi);
            }

            _responseApi.StatusCode = HttpStatusCode.OK;
            _responseApi.IsSuccess = true;
            _responseApi.Result = responseLogin;
            return Ok(_responseApi);
        }

    }
}
