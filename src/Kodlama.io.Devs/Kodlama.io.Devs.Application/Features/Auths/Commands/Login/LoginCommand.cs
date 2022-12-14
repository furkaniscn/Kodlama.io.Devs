using AutoMapper;
using Core.Security.Entities;
using Core.Security.Hashing;
using Core.Security.JWT;
using Kodlama.io.Devs.Application.Features.Auths.Dtos;
using Kodlama.io.Devs.Application.Features.Auths.Rules;
using Kodlama.io.Devs.Application.Services.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kodlama.io.Devs.Application.Features.Auths.Commands.Login
{
    public class LoginCommand : IRequest<TokenDto>
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenDto>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserOperationClaimRepository _userOperationClaimRepository;
            private readonly IMapper _mapper;
            private readonly ITokenHelper _tokenHelper;
            private readonly AuthBusinessRules _authBusinessRules;

            public LoginCommandHandler(IUserRepository userRepository, IUserOperationClaimRepository userOperationClaimRepository, IMapper mapper, ITokenHelper tokenHelper, AuthBusinessRules authBusinessRules)
            {
                _userRepository = userRepository;
                _userOperationClaimRepository = userOperationClaimRepository;
                _mapper = mapper;
                _tokenHelper = tokenHelper;
                _authBusinessRules = authBusinessRules;
            }
            public async Task<TokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                User? user = await _userRepository.GetAsync(u => u.Email == request.Email, include: m => m.Include(c => c.UserOperationClaims).ThenInclude(x => x.OperationClaim));

                List<OperationClaim> operationClaims = new() { };

                foreach (var userOperationClaim in user.UserOperationClaims)
                {
                    operationClaims.Add(userOperationClaim.OperationClaim);
                }

                _authBusinessRules.UserShouldExistWhenRequested(user!);
                _authBusinessRules.VerifyUserPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);

                AccessToken accessToken = _tokenHelper.CreateToken(user, operationClaims);

                TokenDto tokenDto = _mapper.Map<TokenDto>(accessToken);

                return tokenDto;
            }
        }
    }
}