using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardChess.Models;

namespace CardChess.Pieces
{
    public class Bishop : IPiece
    {
        // 비숍의 현재 위치, 소유자, 종류 정의
        public position currentposition { get; set; }
        public playertype owner { get; set; }
        public piecetype type => piecetype.bishop;

        // 생성자: 소유자와 초기 위치 설정
        public bishop(playertype owner, position currentposition)
        {
            owner = owner;
            currentposition = currentposition;
        }

        // 비숍의 이동 및 공격 로직은 동일 (대각선상에 적이 있으면 잡음)
        public list<position> getmovablepositions(gamestate state)
        {
            return getlongrangemoves(state);
        }

        public list<position> getattackablepositions(gamestate state)
        {
            return getlongrangemoves(state);
        }

        // 대각선 4방향으로 장애물을 만날 때까지 탐색하는 로직
        private list<position> getlongrangemoves(gamestate state)
        {
            list<position> positions = new list<position>();

            // 탐색할 대각선 4방향 정의
            int[] drow = { -1, -1, 1, 1 };
            int[] dcol = { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nextrow = currentposition.row + drow[i];
                int nextcol = currentposition.col + dcol[i];

                // 보드 범위를 벗어나기 전까지 해당 방향으로 계속 전진
                while (state.iswithinboard(new position(nextrow, nextcol)))
                {
                    position nextpos = new position(nextrow, nextcol);
                    ipiece target = state.getpieceat(nextpos);

                    if (target == null)
                    {
                        // 빈 칸이면 추가하고 계속 전진
                        positions.add(nextpos);
                    }
                    else
                    {
                        // 기물을 만났을 때: 적군이면 추가하고 중단, 아군이면 바로 중단
                        if (target.owner != this.owner)
                        {
                            positions.add(nextpos);
                        }
                        break;
                    }

                    // 다음 칸으로 좌표 갱신
                    nextrow += drow[i];
                    nextcol += dcol[i];
                }
            }

            return positions;
        }

        // 타겟 좌표가 이동 가능한 리스트에 있는지 확인
        public bool canmove(position target, gamestate state)
        {
            return getmovablepositions(state).contains(target);
        }

        // 타겟 좌표가 공격 가능한 리스트에 있는지 확인
        public bool canattack(position target, gamestate state)
        {
            return getattackablepositions(state).contains(target);
        }
    }
}