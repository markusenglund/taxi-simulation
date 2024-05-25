from manim import *
DEFAULT_FONT_SIZE = 40

Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class TotalCostCalculation(Scene):
    def construct(self):
        self.camera.background_color = "#444444"

        # equationStart = Tex(r"Total cost", r"=", "fare", r"+ (Time to destination \cdot Cost of time)", font_size=DEFAULT_FONT_SIZE).move_to(DOWN*2)
        equationStart = Tex(r"Total cost", r" = Fare", r" + Time to destination",  r" × Cost of time", tex_template=TexFontTemplates.droid_sans ,font_size=DEFAULT_FONT_SIZE)
        # equationWithValues = MathTex(r"= \$12.00", r"+ 0.35 \, hrs", r"× \$141.20/hr", font_size=DEFAULT_FONT_SIZE)
        equationWithValues = Tex(r"= \$12.00", r" + 24 min", r" × \$141.20/hr", tex_template=TexFontTemplates.droid_sans, font_size=DEFAULT_FONT_SIZE)
        equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationWithValues2 = Tex(r"= \$12.00", r" + \$56.67", tex_template=TexFontTemplates.droid_sans, font_size=DEFAULT_FONT_SIZE)
        equationWithValues2.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationResult = Tex(r"= \$68.67", tex_template=TexFontTemplates.droid_sans, font_size=DEFAULT_FONT_SIZE)
        equationResult.next_to(equationWithValues2, DOWN).align_to(equationStart[1], LEFT)
        # equationStart[0].set_color(YELLOW)
        # finalResultAtTheTop[0].set_color(YELLOW)

        # equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        # equationCombined.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        # equationSimplified.next_to(equationWithValues, DOWN).align_to(equationStart[1], LEFT)
        # self.play(Write(regularText))
        self.play(GrowFromCenter(equationStart[0]))
        self.play(GrowFromEdge(equationStart[1], LEFT))
        # self.wait(1)
        self.play(GrowFromEdge(equationStart[2], LEFT))
        
        # self.wait(1)
        self.play(GrowFromEdge(equationStart[3], LEFT))
        # self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[1], equationWithValues[0]),
            TransformFromCopy(equationStart[2], equationWithValues[1]),
            TransformFromCopy(equationStart[3], equationWithValues[2]),
            lag_ratio=0.1,
            run_time=1
        ))
        self.wait(1)
        self.play(ReplacementTransform(equationWithValues, equationWithValues2))


        self.play(TransformFromCopy(equationWithValues2, equationResult))
        self.wait(3)
